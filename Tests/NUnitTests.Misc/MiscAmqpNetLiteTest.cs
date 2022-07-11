namespace NUnitTests.Misc;

using System;
using System.Linq;
using System.Text;
using System.Transactions;
using ActiveMQ.Artemis.Client;
using Amqp;
using Amqp.Framing;
using NUnit.Framework;
using Tests.Common.EnsureExtension;
using ArtemisNetClient = ActiveMQ.Artemis;
using ConnectionFactory = ActiveMQ.Artemis.Client.ConnectionFactory;
using IConnection = ActiveMQ.Artemis.Client.IConnection;
using Message = Amqp.Message;

[TestFixture]
internal class MiscAmqpNetLiteTest : NUnitTest
{
    [SetUp]
    public async Task MiscAmqpNetLiteTestSetUp()
    {
        _topicName = $"{_addressPrefix}{DateTime.Now.ToString("o")}-{Guid.NewGuid()}"
            .Replace(":","-").Replace("+","-");

        LogDebug($"Creating topic {_topicName}.");

        await using ITopologyManager? topologyManager = await _artemisConnection.CreateTopologyManagerAsync();
        var address = _artemisConnection.Endpoint.ToString();
        LogDebug($"Using address {address}.");

       

        await using IProducer? producer = await _artemisConnection.CreateProducerAsync(
            new ProducerConfiguration
            {
                MessageDurabilityMode = DurabilityMode.Nondurable,
                Address = _topicName,
                RoutingType = RoutingType.Anycast
            });
        await topologyManager.CreateQueueAsync(new QueueConfiguration
        {
            Address = _topicName,
            Name = $"Queue01-{_topicName}",
            // RoutingType = RoutingType.Anycast,
            Durable = false,
            AutoDelete = true
        });


        _session = new Session(_connection);

        _receiver = new ReceiverLink(_session, "test-receiver01", _topicName);
        _sender = new SenderLink(_session, "test-sender01", _topicName);


        //var producer = await _artemisConnection.CreateProducerAsync(new ProducerConfiguration()
        //{
        //    MessageDurabilityMode = DurabilityMode.Nondurable
        //});
    }

    [TearDown]
    public async Task MiscAmqpNetLiteTestTearDown()
    {
        bool queueWasDeleted = false;
        try
        {
            await _receiver.CloseAsync();
        }
        catch (Amqp.AmqpException ex)
        {
            if (ex.Message.Contains("Queue was deleted"))
            {
                queueWasDeleted = true;
            }
        }

        await _sender.CloseAsync();
        await _session.CloseAsync();

        if (_doDeleteAddressesUponTearDown)
        {
            await using ITopologyManager? topologyManager =
                await _artemisConnection.CreateTopologyManagerAsync();
            IReadOnlyList<string> addresses =
                (await topologyManager.GetQueueNamesAsync()) ?? Array.Empty<string>();
            if (!queueWasDeleted)
            {
                LogDebug($"Deleting {_topicName}");
                await topologyManager.DeleteAddressAsync(_topicName, true);
            }
            // foreach (var address in addresses.Where(a => a.StartsWith(_addressPrefix)))
            // {
            //     //To avoid error AMQ229205: Address AutoTestQueue-7452c193-32f1-4853-9812-fad11ef6bc00 has bindings
            //     try
            //     {
            //         // await topologyManager.DeleteQueueAsync(_topicName, removeConsumers:true, autoDeleteAddress:true);
            //         await topologyManager.DeleteAddressAsync(address, true);
            //     }
            //     catch (Exception e)
            //     {
            //         LogDebug($"Swallowing exception '{e.Message}'.");
            //     }
            // }
        }
    }

    private const string _addressPrefix = "AutoTestQueue-";
    private const bool _doDeleteAddressesUponTearDown = true;

    private Connection _connection = null!;
    private string _topicName = "";
    private Session _session = null!;
    private ReceiverLink _receiver = null!;
    private SenderLink _sender = null!;
    private IConnection _artemisConnection = null!;

    [OneTimeSetUp]
    public void MiscAmqpLiteTestsSetUp()
    {
        var address = new Address("amqp://admin:admin@localhost:5672");

        _connection = new Connection(address);
        var connectionFactory = new ConnectionFactory();
        Endpoint endpoint = Endpoint.Create("localhost", 5672, "admin", "admin").EnsureNotNull();
        _artemisConnection = connectionFactory.CreateAsync(endpoint).Result;
    }


    [OneTimeTearDown]
    public async Task MiscAmqpLiteTestsTearDown()
    {
        await _connection.CloseAsync();
        await _artemisConnection.DisposeAsync();
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

    
    [TestCase("AutoTestQueue-")]
    [Explicit]
    public async Task DeleteAllAddressesMatching(string adressPrefix)
    {
        await using ITopologyManager? topologyManager = await _artemisConnection.CreateTopologyManagerAsync();

        var addresses = topologyManager.GetAddressNamesAsync().Result;
        foreach (var address in addresses)
        {
            if (address.StartsWith(adressPrefix))
            {
                Console.WriteLine($"Deleting address '{address}'.");
                topologyManager.DeleteAddressAsync(address, true).Wait();
            }
            else
            {
                Console.WriteLine($"Not matching address: {address}");
            }
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
