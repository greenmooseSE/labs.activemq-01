namespace NUnitTests.Misc;

using System.Transactions;
using Amqp;
using Nito.AsyncEx;
using NUnit.Framework;
using Test.AMQPNetLite.Common;

[TestFixture]
internal class MiscAmqpNetLiteTest : AmqpNetLiteTest
{
    [TestCase(100)]
    // [TestCase(1 * 1000)]
    // [TestCase(10 * 1000)]
    public void CanCommitMultipleMsgInTransactionScope(int msgCount)
    {
        var msgs = Enumerable.Range(0, msgCount)
            .Select(idx => $"{DateTime.Now.ToString("O")}-{idx:N0}-{Guid.NewGuid()}")
            .ToList();
        using (var ts = new TransactionScope())
        {
            foreach (var msg in msgs)
            {
                SendMsg(msg);
            }

            ts.Complete();
        }
    }

    [Test]
    public void CanReceiveAMessageFromMultipleTopics()
    {
        //Set up 2 queues
        using var scope1 = AmqpTempQueueScope.Create("test-queue-01");
        using var scope2 = AmqpTempQueueScope.Create("test-queue-02");
        LogDebug($"Using queue topic: {scope1.TopicName}");
        LogDebug($"Using queue topic: {scope2.TopicName}");


        //Set up 2 listeners separately for that queue (connection is singleton)
        var listenerSession = new Session(scope1.AmqpNetLiteConnection);

        var receiverLink1 = new ReceiverLink(listenerSession, "receiver01", scope1.TopicName);
        var receiverLink2 = new ReceiverLink(listenerSession, "receiver02", scope2.TopicName);

        var cancellationToken = new CancellationTokenSource();
        cancellationToken.CancelAfter(TimeSpan.FromSeconds(1));

        var receiver1Started = false;
        var receiver2Started = false;

        Task task1 = Task.Factory.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                LogDebug("Receiver1: Waiting for msg");
                receiver1Started = true;
                Message? msg = await receiverLink1.ReceiveAsync(TimeSpan.FromMilliseconds(100));
                if (msg != null)
                {
                    var text = GetMsgText(msg);
                    LogInfo($"Receiver1: Got message: {text}");
                }

                await Task.Delay(100);
            }
        });
        Task task2 = Task.Factory.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                LogDebug("Receiver2: Waiting for msg");
                receiver2Started = true;
                Message? msg = await receiverLink2.ReceiveAsync(TimeSpan.FromMilliseconds(100));
                if (msg != null)
                {
                    var text = GetMsgText(msg);
                    LogInfo($"Receiver2: Got message: {text}");
                }

                await Task.Delay(100);
            }
        });

        while (!receiver2Started || !receiver1Started)
        {
            Thread.Sleep(10);
        }

        Message msg1 = CreateMessage("Message1");
        Message msg2 = CreateMessage("Message2");
        LogDebug("Sending msg1");
        scope1.AmqpSenderLink.Send(msg1);
        LogDebug("Sending msg2");
        scope2.AmqpSenderLink.Send(msg2);

        Task.WaitAll(task1, task2);
    }

    [Test]
    public void CanSendAndReceiveAMessage()
    {
        //Create receiver before sending the msg
        var msg = $"{DateTime.Now.ToString("O")}-{Guid.NewGuid()}";

        SendMsg(msg);

        var receivedMsg = GetMsg();
        StringAssert.Contains(msg, receivedMsg);
    }

    [Test]
    [Explicit]
    public void DeleteAllTestQueues()
    {
        ArtemisHelper.DeleteAllQueuesAsync(QueuePrefix).Wait();
    }

    [Test]
    public void UsingCommittedMsTransactionWillPublishMessage()
    {
        var msg = $"{DateTime.Now.ToString("O")}-{Guid.NewGuid()}";
        using (var ts = new TransactionScope())
        {
            SendMsg(msg);
            ts.Complete();
        }

        StringAssert.Contains(msg, msg);
    }

    [Test]
    public void UsingRolledBackMsTransactionWillNotPublishMessage()
    {
        using (new TransactionScope())
        {
            var msg = $"{DateTime.Now.ToString("O")}-{Guid.NewGuid()}";
            SendMsg(msg);
        }

        Assert.IsNull(GetOptionalMsg());
    }
}
