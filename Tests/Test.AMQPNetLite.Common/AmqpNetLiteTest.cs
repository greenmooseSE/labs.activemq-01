namespace Test.AMQPNetLite.Common;

using System.Text;
using Amqp;
using Amqp.Framing;
using global::Common.EnsureExtension;
using NUnit.Framework;
using Tests.Common;

[TestFixture]
public abstract class AmqpNetLiteTest : UnitTest
{
    [SetUp]
    public void AmqpNetLiteTestSetUp()
    {
        QueueScope = AmqpTempQueueScope.Create(QueuePrefix);
    }

    [TearDown]
    public void AmqpNetLiteTestTearDown()
    {
        QueueScope.Dispose();
    }

    private const string _queuePrefix = "amqpnetlite-test-";

    protected static Message CreateMessage(string messageText)
    {
        var message = new Message {BodySection = new Data {Binary = Encoding.UTF8.GetBytes(messageText)}};
        Assert.IsNull(message.Header);
        //We don't want to keep msgs during restart
        message.Header = new Header {Durable = false};
        return message;
    }

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

        return GetMsgText(message);
    }

    protected string GetMsgText(Message message)
    {
        var msgText = Encoding.UTF8.GetString((byte[])message.Body);
        LogDebug($"Got message: {msgText}");
        return msgText;
    }


    protected Message? GetOptionalMsgObj()
    {
        Message? msg = QueueScope.AmqpReceiverLink.Receive(TimeSpan.FromMilliseconds(10));
        if (msg == null)
        {
            return null;
        }

        return msg;
    }

    protected void SendMsg(string msg)
    {
        Message message = CreateMessage(msg);
        QueueScope.AmqpSenderLink.Send(message);
    }

    protected string QueuePrefix => _queuePrefix;

    public AmqpTempQueueScope QueueScope { get; private set; }
}
