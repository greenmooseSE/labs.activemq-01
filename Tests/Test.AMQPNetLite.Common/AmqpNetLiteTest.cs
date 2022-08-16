namespace Test.AMQPNetLite.Common;

using System.Text;
using Amqp;
using Amqp.Framing;
using NUnit.Framework;
using Tests.Common;
using Tests.Common.EnsureExtension;

[TestFixture]
public abstract class AmqpNetLiteTest : UnitTest
{
    [SetUp]
    public void AmqpNetLiteTestSetUp()
    {
        _queueScope = AmqpTempQueueScope.Create(QueuePrefix);
    }

    [TearDown]
    public void AmqpNetLiteTestTearDown()
    {
        _queueScope.Dispose();
    }

    private const string _queuePrefix = "amqpnetlite-test-";
    private AmqpTempQueueScope _queueScope;

    protected string GetMsg()
    {
        var msgText = GetOptionalMsg().EnsureNotNull();
        return msgText;
    }

    protected string? GetOptionalMsg()
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


    protected Message? GetOptionalMsgObj()
    {
        Message? msg = _queueScope.AmqpReceiverLink.Receive(TimeSpan.FromMilliseconds(10));
        if (msg == null)
        {
            return null;
        }

        return msg;
    }

    protected void SendMsg(string msg)
    {
        var message = new Message {BodySection = new Data {Binary = Encoding.UTF8.GetBytes(msg)}};
        Assert.IsNull(message.Header);
        //We don't want to keep msgs during restart
        message.Header = new Header {Durable = false};
        _queueScope.AmqpSenderLink.Send(message);
    }

    protected string QueuePrefix => _queuePrefix;
}
