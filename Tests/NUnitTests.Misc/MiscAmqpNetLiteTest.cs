namespace NUnitTests.Misc;

using System.Transactions;
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
