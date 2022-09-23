namespace NUnitTests.Misc;

using System.Diagnostics;
using System.Text.Json;
using System.Transactions;
using ActiveMQ.Artemis.Client;
using NUnit.Framework;
using Tests.Common;
using Transaction = ActiveMQ.Artemis.Client.Transactions.Transaction;

[TestFixture]
[Ignore("This fixture does not clean up created queues")]
internal class MiscTest : NUnitTest
{
    [SetUp]
    public async Task MiscTestSetUp()
    {
        Endpoint[] endpoints = {Endpoint.Create("localhost", 5672, "admin", "admin")};
        _connectionFactory = new ConnectionFactory();
        _connection = await _connectionFactory.CreateAsync(endpoints);

        _queueName = $"AutoTestQueue-{Guid.NewGuid()}";
        _consumer = await _connection.CreateConsumerAsync(_queueName, RoutingType.Anycast);
        _producer = await _connection.CreateProducerAsync(_queueName, RoutingType.Anycast);
    }

    [TearDown]
    public async Task MiscTestTearDown()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _connection = null!;
        _connectionFactory = null!;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_consumer != null)
        {
            await _consumer.DisposeAsync();
        }

        _consumer = null!;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_producer != null)
        {
            await _producer.DisposeAsync();
        }

        _producer = null!;
    }

    private IConnection _connection = null!;

    private ConnectionFactory _connectionFactory = null!;
    private IConsumer _consumer = null!;
    private IProducer _producer = null!;
    private string _queueName = "";

    private async Task<string?> ReceiveMsg(TimeSpan? timeout = null)
    {
        TimeSpan timeoutToUse = timeout ?? TimeSpan.FromMilliseconds(1);

        var cancelTokenSrc = new CancellationTokenSource(timeoutToUse);


        // Task.Run(async () =>
        // {
        Message msg;
        try
        {
            msg = await _consumer.ReceiveAsync(cancelTokenSrc.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        var msgBody = msg.GetBody<string>();
        LogDebug($"Got message : {msgBody}");
        // LogDebug($"Got message {JsonSerializer.Serialize(msg)}");
        return msgBody;
        // }
    }

    private async Task SendMsg(string msgContent = "bar")
    {
        await _producer.SendAsync(new Message(JsonSerializer.Serialize(new {foo = msgContent}))
        {
            CorrelationId = "Test01", CreationTime = DateTime.Now, DurabilityMode = DurabilityMode.Durable
        });
    }

    // [Test]
    // public void CanResolveMessageProducer()
    // {
    //     MessageProducer inst = Resolve<MessageProducer>();
    //     Assert.IsNotNull(inst);
    // }


    [Test]
    public async Task CanSendAndReceiveAMessage()
    {
        var msgReceived = "";
        var consumerTask = Task.Run(async () =>
        {
            Message? msg = await _consumer.ReceiveAsync();
            msgReceived = $"Got message {JsonSerializer.Serialize(msg)}";
            Console.WriteLine(msgReceived);
        });
        await _producer.SendAsync(new Message(JsonSerializer.Serialize(new {foo = "bar"}))
        {
            CorrelationId = "Test01", CreationTime = DateTime.Now, DurabilityMode = DurabilityMode.Durable
        });

        await consumerTask;

        StringAssert.Contains("Test01", msgReceived);
    }

    [Test]
    public async Task DoesNotSupportArtemisTransactionScopeForSending()
    {
        //Sanity assert, ensure we can send and get a message
        await SendMsg("Msg01");
        var msgReceived = await ReceiveMsg();
        StringAssert.Contains("Msg01", msgReceived);

        //Sanity assert 2, ensure receiving without sending returns null
        Assert.IsNull(await ReceiveMsg());

        //Sanity assert 3, ensure a message inside a committed trans scope will be sent
        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await using var trans = new Transaction();
            await SendMsg("Msg02");
            await trans.CommitAsync();
        });
        StringAssert.Contains("Msg02", await ReceiveMsg());

        //Act, send a message within a trans and rollback
        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await using var trans = new Transaction();
            await SendMsg("Msg03");
        });
        //Assert, message is still available - If it would work as expected msg should be null
        StringAssert.Contains("Msg03", await ReceiveMsg());
    }


    [Test]
    public async Task DoesNotSupportMicrosoftTransactionScopeForSending()
    {
        //Sanity assert, ensure we can send and get a message
        await SendMsg("Msg01");
        var msgReceived = await ReceiveMsg();
        StringAssert.Contains("Msg01", msgReceived);

        //Sanity assert 2, ensure receiving without sending returns null
        Assert.IsNull(await ReceiveMsg());

        //Sanity assert 3, ensure a message inside a committed trans scope will be sent
        using (var trans = new TransactionScope())
        {
            await SendMsg("Msg02");
            trans.Complete();
        }

        StringAssert.Contains("Msg02", await ReceiveMsg());

        //Act, send a message within a trans and rollback
        using (new TransactionScope())
        {
            await SendMsg("Msg02");
        }

        //Assert, message is still available
        Assert.IsNotNull(await ReceiveMsg());
    }

    [Test]
    public async Task NonDurableVsDurableSpeedTest()
    {
        for (var i = 0; i < 10; ++i)
        {
            var sw1 = Stopwatch.StartNew();
            await _producer.SendAsync(new Message(JsonSerializer.Serialize(new {foo = "bar"}))
            {
                CorrelationId = "Test01",
                CreationTime = DateTime.Now,
                DurabilityMode = DurabilityMode.Nondurable
            });
            sw1.Stop();

            var sw2 = Stopwatch.StartNew();
            await _producer.SendAsync(new Message(JsonSerializer.Serialize(new {foo = "bar"}))
            {
                CorrelationId = "Test01",
                CreationTime = DateTime.Now,
                DurabilityMode = DurabilityMode.Durable
            });
            sw2.Stop();


            Console.WriteLine($"sw1: {sw1.ElapsedMilliseconds:N} sw1: {sw2.ElapsedMilliseconds:N}");
        }
    }
}
