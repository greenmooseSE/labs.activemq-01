namespace NUnitTests.Misc.MessageServiceTests;

using System.Collections.Concurrent;
using Amqp;
using AmqpNetLite.Common;
using FluentAssertions;
using NUnit.Framework;
using Test.AMQPNetLite.Common;

[TestFixture]
internal class MiscMessageServiceTest : MessageServiceTest
{
    [Test]
    public void AcceptedMessageIsNotAvailableEvenIfReceiverLinkIsClosed()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();

        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);

        SendMsg("Msg1");
        SendMsg("Msg2");

        var processedMsgs = new ConcurrentBag<string>();

        void onMsg(Message msg)
        {
            processedMsgs.Add(GetMsgText(msg));
        }

        var acceptedMsgs = 0;

        void OnMsgWithAccept(Message msg, ReceiverLink receiverLink)
        {
            processedMsgs.Add(GetMsgText(msg));
            if (Interlocked.Increment(ref acceptedMsgs) == 1)
            {
                receiverLink.Accept(msg);
            }
            else
            {
                LogDebug($"Not accepting the message '{GetMsgText(msg)}'.");
            }
        }

        sut1.OnProcessMessageEx += OnMsgWithAccept;
        sut2.OnProcessMessage += onMsg;
        ActByCallingProcessMessages(sut1, QueueScope.TopicName);
        ActByCallingProcessMessages(sut2, QueueScope.TopicName);

        //Sanity assert
        processedMsgs.Should().HaveCount(2);

        //Act
        sut1.CloseReceiverLinks();

        //Actual assert - we should only have 1 msg left in queue
        ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        processedMsgs.Should().HaveCount(3);
    }

    [Test]
    public void CreditForReceiverLink1ReservesAllMessagesFromBrokerForThatCredit()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();
        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);
        var processedMsgsSut1 = new ConcurrentBag<string>();
        var processedMsgsSut2 = new ConcurrentBag<string>();
        sut1.SetQueueNames(new[]{QueueScope.TopicName});
        sut2.SetQueueNames(new[]{QueueScope.TopicName});
        sut1.OnProcessMessageEx += (msg, rlink) =>
        {
            var msgText = GetMsgText(msg);
            processedMsgsSut1.Add(msgText);
            LogDebug($"SUT1 Got msg '{msgText}' from link '{rlink.Name}'.");

            // ReSharper disable once AccessToDisposedClosure
            ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        };
        sut2.OnProcessMessageEx += (msg, rlink) =>
        {
            var msgText = GetMsgText(msg);
            LogDebug($"SUT2 Got msg '{msgText}' from link '{rlink.Name}'.");
            processedMsgsSut2.Add(msgText);
        };

        SendMsg("Msg1");
        SendMsg("Msg2");

        //act
        ActByCallingProcessMessagesKeepCurrentQueues(sut1);

        //assert
        processedMsgsSut1.Should().HaveCount(1);
        processedMsgsSut2.Should().HaveCount(1);

        Assert.Fail("TODO:(B81ISE/20220823) Complete test CreditForReceiverLink1ReservesAllMessagesFromBrokerForThatCredit");
    }

    [Test]
    public void AcceptedMessageThatIsRejectedDoesNotBecomeAvailableUntilReceiverLinkIsClosed()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();

        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);

        SendMsg("Msg1");
        SendMsg("Msg2");

        var processedMsgs = new ConcurrentBag<string>();

        void onMsg(Message msg)
        {
            processedMsgs.Add(GetMsgText(msg));
        }

        var acceptedMsgs = 0;

        void OnMsgWithAcceptAndRejectForSut1(Message msg, ReceiverLink receiverLink)
        {
            processedMsgs.Add(GetMsgText(msg));
            if (Interlocked.Increment(ref acceptedMsgs) == 1)
            {
                receiverLink.Accept(msg);
                receiverLink.Reject(msg);
            }
            else
            {
                LogDebug($"Not accepting the message '{GetMsgText(msg)}'.");
            }
        }

        sut1.OnProcessMessageEx += OnMsgWithAcceptAndRejectForSut1;
        sut2.OnProcessMessage += onMsg;
        ActByCallingProcessMessages(sut1, QueueScope.TopicName);
        ActByCallingProcessMessages(sut2, QueueScope.TopicName);

        //Sanity assert
        processedMsgs.Should().HaveCount(2);
        

        //Act
        sut1.CloseReceiverLinks();

        //Actual assert - we should only have 1 msg left in queue
        ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        processedMsgs.Should().HaveCount(3);
    }

    [Test]
    public void CanResolveMsgService()
    {
        Assert.NotNull(NewMessageServiceInstance());
    }

    [Test]
    public void CanStartAndStopService()
    {
        MessageService service = NewMessageServiceInstance();
        ActByCallingProcessMessages(service);
    }

    [Test]
    public void GivenAMessageExistsInQueueWhenProcessMessagesAsyncIsInvokedItShouldProcessTheMessage()
    {
        using var queueScope = AmqpTempQueueScope.Create("testqueue");
        Message msg = CreateMessage("Test message");

        queueScope.AmqpSenderLink.Send(msg);

        MessageService sut = NewMessageServiceInstance();

        Message? fetchedMessage = null;
        sut.OnProcessMessage += theMsg =>
        {
            fetchedMessage = theMsg;
        };

        ActByCallingProcessMessages(sut, queueScope.TopicName);
        fetchedMessage.Should().NotBeNull();
    }

    [Test]
    public void GivenWeHave3QueuesWith1MsgEachWhenProcessMessagesAsyncIsInvokedItShouldProcessAllQueues()
    {
        using var queueScope1 = AmqpTempQueueScope.Create("testqueue");
        using var queueScope2 = AmqpTempQueueScope.Create("testqueue");
        using var queueScope3 = AmqpTempQueueScope.Create("testqueue");
        var queueNames = new[] {queueScope1.TopicName, queueScope2.TopicName, queueScope3.TopicName};

        MessageService sut = NewMessageServiceInstance();
        var processedMsgs = new ConcurrentBag<string>();

        var msgTexts = queueNames.Select(qn => qn).ToList();
        var msgs = msgTexts.Select(t => CreateMessage(t)).ToList();
        queueScope1.AmqpSenderLink.Send(msgs[0]);
        queueScope2.AmqpSenderLink.Send(msgs[1]);
        queueScope3.AmqpSenderLink.Send(msgs[2]);

        sut.OnProcessMessage += msg =>
        {
            processedMsgs.Add(GetMsgText(msg));
        };

        //act
        ActByCallingProcessMessages(sut, queueNames);

        //assert
        processedMsgs.Should().BeEquivalentTo(msgTexts);
    }

    [Test]
    public void NonAcceptedMessageBecomesAvailableInQueueWhenReceiverLinkIsClosed()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();

        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);

        var msgText = "Some msg";
        SendMsg(msgText);

        var processedMsgs = new ConcurrentBag<string>();

        void onMsg(Message msg)
        {
            processedMsgs.Add(GetMsgText(msg));
        }

        sut1.OnProcessMessage += onMsg;
        sut2.OnProcessMessage += onMsg;
        ActByCallingProcessMessages(sut1, QueueScope.TopicName);
        ActByCallingProcessMessages(sut2, QueueScope.TopicName);

        //Sanity assert
        processedMsgs.Should().HaveCount(1);

        //Act
        sut1.CloseReceiverLinks();

        //Actual assert
        ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        processedMsgs.Should().HaveCount(2);
    }

    [Test]
    public void NonAcceptedMessageBecomesAvailableInQueueIfSessionIsClosed()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();

        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);

        var msgText = "Some msg";
        SendMsg(msgText);

        var processedMsgs = new ConcurrentBag<string>();

        void onMsg(Message msg)
        {
            processedMsgs.Add(GetMsgText(msg));
        }

        sut1.OnProcessMessage += onMsg;
        sut2.OnProcessMessage += onMsg;
        ActByCallingProcessMessages(sut1, QueueScope.TopicName);
        ActByCallingProcessMessages(sut2, QueueScope.TopicName);
        //Sanity assert
        processedMsgs.Should().HaveCount(1);

        //Act
        sut1.CloseSession();

        //Actual assert
        ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        processedMsgs.Should().HaveCount(2);
    }

    [Test]
    public void NonAcceptedMessagesAreStillInQueueAfterFirstMsgServiceInstanceIsDisposed()
    {
        MessageService sut = NewMessageServiceInstance();

        var msgText = "Some msg";
        SendMsg(msgText);

        var processedMsgs = new ConcurrentBag<string>();
        sut.OnProcessMessage += msg =>
        {
            processedMsgs.Add(GetMsgText(msg));
        };

        ActByCallingProcessMessages(sut, QueueScope.TopicName);
        processedMsgs.Should().HaveCount(1);

        DisposeCurrentMessageServiceInstance();

        sut = NewMessageServiceInstance();
        sut.OnProcessMessage += msg =>
        {
            processedMsgs.Add(GetMsgText(msg));
        };
        ActByCallingProcessMessages(sut, QueueScope.TopicName);
        processedMsgs.Should().HaveCount(2);
    }
}
