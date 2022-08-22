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

        var sut = NewMessageServiceInstance();
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
        ActByCallingProcessMessages(sut,queueNames);

        //assert
        processedMsgs.Should().BeEquivalentTo(msgTexts);
    }
}
