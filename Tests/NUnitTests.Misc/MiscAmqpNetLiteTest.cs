namespace NUnitTests.Misc;

using System;
using System.Linq;
using System.Text;
using System.Transactions;
using Amqp;
using Amqp.Framing;
using NUnit.Framework;
using Tests.Common.EnsureExtension;

[TestFixture]
internal class MiscAmqpNetLiteTest : NUnitTest
{
    [SetUp]
    public void MiscAmqpNetLiteTestSetUp()
    {
        _topicName = $"AutoTest-{DateTime.Now.ToString("o")}-{Guid.NewGuid()}";
        _session = new Session(_connection);
        
        _receiver = new ReceiverLink(_session, "test-receiver01", _topicName);
        _sender = new SenderLink(_session, "test-sender01", _topicName);
        
    }

    [TearDown]
    public void MiscAmqpNetLiteTestTearDown()
    {
        _receiver.Close();
        _sender.Close();
        _session.Close();
    }

    private Connection _connection = null!;
    private string _topicName = "";
    private Session _session = null!;
    private ReceiverLink _receiver = null!;
    private SenderLink _sender = null!;

    [OneTimeSetUp]
    public void MiscAmqpLiteTestsSetUp()
    {
        var address = new Address("amqp://admin:admin@localhost:5672");

        _connection = new Connection(address);
    }


    [OneTimeTearDown]
    public void MiscAmqpLiteTestsTearDown()
    {
        _connection.Close();
    }

    private void SendMsg(string msg)
    {
        var message = new Message {BodySection = new Data {Binary = Encoding.UTF8.GetBytes(msg)}};
        Assert.IsNull(message.Header);
        //We don't want to keep msgs during restart
        message.Header = new Header {Durable = false};
        _sender.Send(message);
    }


    private string GetMsg()
    {
        var msgText = GetOptionalMsg().EnsureNotNull();
        return msgText;
    }

    private string? GetOptionalMsg()
    {
        Message? message = GetOptionalMsgObj();
        if (message == null)
        {
            return null;
        }

        var msgText = Encoding.UTF8.GetString((byte[])message.Body);
        LogDebug($"Got message: {msgText}");
        return msgText;
    }

    private Message? GetOptionalMsgObj()
    {
        Message? msg = _receiver.Receive(TimeSpan.FromMilliseconds(10));
        if (msg == null)
        {
            return null;
        }

        return msg;
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
    public void UsingRolledBackMsTransactionWillNotPublishMessage()
    {
        using (new TransactionScope())
        {
            var msg = $"{DateTime.Now.ToString("O")}-{Guid.NewGuid()}";
            SendMsg(msg);
        }

        Assert.IsNull(GetOptionalMsg());
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

    [TestCase(100)]
    [TestCase(1*1000)]
    [TestCase(10*1000)]
    public void CanCommitMultipleMsgInTransactionScope(int msgCount)
    {
        var msgs = Enumerable.Range(0, msgCount)
            .Select(idx => $"{DateTime.Now.ToString("O")}-{idx:N0}-{Guid.NewGuid()}")
            .ToList();
        using (var ts = new TransactionScope())
        {
            foreach(var msg in msgs)
                SendMsg(msg);
            ts.Complete();
        }
        
    }
}
