namespace NUnitTests.Misc.MessageServiceTests;

using System.Collections.Concurrent;
using Amqp;
using Amqp.Types;
using AmqpNetLite.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
                Logger.LogInformation($"Not accepting the message '{GetMsgText(msg)}'.");
            }
        }

        sut1.OnProcessMessageEx += OnMsgWithAccept;
        sut2.OnProcessMessage += onMsg;
        //We use credit limit 2 to ensure both messages are consumed by sut1.
        ActByCallingProcessMessages(sut1, QueueScope.TopicName, creditLimit:2);
        processedMsgs.Should().HaveCount(2);

        //Sanity assert - no messages are available for sut2
        ActByCallingProcessMessages(sut2, QueueScope.TopicName);
        processedMsgs.Should().HaveCount(2);

        //Act
        sut1.CloseReceiverLinks();

        //Actual assert - msg1, and only msg1, should re-appear in queue and be processed by sut2
        ActByCallingProcessMessagesKeepCurrentQueues(sut2);
        processedMsgs.Should().HaveCount(3);
        processedMsgs.Where(msg => msg == "Msg2").Should().HaveCount(2);
    }

    [Test]
    public void CreditForReceiverLink1ReservesAllMessagesFromBrokerForThatCredit()
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();
        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);
        var processedMsgsSut1 = new ConcurrentBag<string>();
        var processedMsgsSut2 = new ConcurrentBag<string>();
        sut1.SetQueueNames(new[] {QueueScope.TopicName});
        sut2.SetQueueNames(new[] {QueueScope.TopicName});
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
        sut1.SetCreditLimit(1);

        //act
        ActByCallingProcessMessagesKeepCurrentQueues(sut1);

        //assert
        processedMsgsSut1.Should().HaveCount(1);
        processedMsgsSut2.Should().HaveCount(1);
    }


    [TestCase(true, false)]
    [TestCase(false, true)]
    public void MessageThatIsRejectedEndsUpInDeadLetterQueueImmediately(bool doAcceptItBeforeReject, bool isMsgExpectedToBeInDlq)
    {
        using MessageService sut1 = NewMessageServiceInstanceNoAutoDispose();

        using MessageService sut2 = NewMessageServiceInstanceNoAutoDispose();
        using MessageService sutDlq = NewMessageServiceInstanceNoAutoDispose();
        sut1.Should().NotBeSameAs(sut2);

        var dlqMsgText = $"Msg1-{Guid.NewGuid()}";

        SendMsg(dlqMsgText);

        var processedMsgs = new ConcurrentBag<string>();

        var acceptedMsgs = 0;

        sut1.OnProcessMessageEx += OnMsgWithAcceptAndReject;
        sut2.OnProcessMessageEx += OnMsgWithAcceptAndReject;
        bool dlqMsgWasFound = false;
        sutDlq.OnProcessMessageEx += (msg, receiverLink) =>
        {
            Logger.LogInformation($"Got msg {GetMsgText(msg)}");
            if (GetMsgText(msg) == dlqMsgText)
            {
                dlqMsgWasFound = true;
                receiverLink.Accept(msg);
                Logger.LogInformation($"Accepted msg {GetMsgText(msg)}");
            }
        };

        //consume any existing msgs in DLQ

        void OnMsgWithAcceptAndReject(Message msg, ReceiverLink receiverLink)
        {
            using var scope1 = Logger.BeginScope(nameof(OnMsgWithAcceptAndReject));
            Logger.LogInformation($"Got msg({GetMsgText(msg)}).");
            //Expect only 1 msg to be processed
            processedMsgs.Add(GetMsgText(msg));
            if (Interlocked.Increment(ref acceptedMsgs) == 1)
            {
                if (doAcceptItBeforeReject)
                {
                    receiverLink.Accept(msg);
                }
                Logger.LogInformation($"Rejecting msg {GetMsgText(msg)}");
                receiverLink.Reject(msg);
                // receiverLink.Modify(msg, false, true);
            }
            else
            {
                receiverLink.Accept(msg);
                Logger.LogInformation($"Not rejecting msg {GetMsgText(msg)}");
            }
        }

        //ACT - Fetch first message in sut1 (which rejects it)
        ActByCallingProcessMessages(sut1, QueueScope.TopicName, creditLimit: 1, actLogScopeName: "sut1");
        processedMsgs.Should().HaveCount(1);

        //Verify only Msg2 is available for sut2
        SendMsg("Msg2");
        ActByCallingProcessMessages(sut2, QueueScope.TopicName, creditLimit: 1, actLogScopeName: "sut2");
        processedMsgs.Should().HaveCount(2);

        //ASSERT - Verify we have Msg1 in DLQ (it can take a while ~1s for msg to end up there)
        //  Since DLQ my contain many non related msgs, we need to bump act duration and stop when we found our msg
        CancellationTokenSource? cancellationToken= new CancellationTokenSource(TimeSpan.FromSeconds(3));
        ActCancellationTokenParamCb = () => cancellationToken;
        var wasFoundTask = Task.Factory.StartNew(() =>
        {
            while (cancellationToken==null || !cancellationToken.IsCancellationRequested)
            {
                if (cancellationToken!=null && dlqMsgWasFound)
                {
                    cancellationToken.Cancel();
                }

            }
        });

        ActByCallingProcessMessages(sutDlq, "DLQ", creditLimit: 10*1000, actLogScopeName: "sutDlq");
        wasFoundTask.Wait();

        dlqMsgWasFound.Should().Be(isMsgExpectedToBeInDlq);
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
    public void NonAcceptedMessagesArePutBackInQueueAfterFirstMsgServiceInstanceIsDisposed()
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
