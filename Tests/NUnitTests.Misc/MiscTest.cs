﻿namespace NUnitTests.Misc;

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using ActiveMQ.Artemis.Client;
using ActiveMqLabs01.Common;
using NUnit.Framework;
using Tests.Common;

[TestFixture]
internal class MiscTest : NUnitTest, ICommonTest
{
    [SetUp]
    public async Task MiscTestSetUp()
    {
        Endpoint[] endpoints = {Endpoint.Create(host: "localhost", port: 5672, "admin", "admin")};
        _connectionFactory = new ConnectionFactory();
        _connection = await _connectionFactory.CreateAsync(endpoints);

        _consumer = await _connection.CreateConsumerAsync("a1", RoutingType.Anycast);
        _producer = await _connection.CreateProducerAsync("a1", RoutingType.Anycast);
    }

    [TearDown]
    public async Task MiscTestTearDown()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _connection = null;
        _connectionFactory = null;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_consumer != null)
        {
            await _consumer.DisposeAsync();
        }

        _consumer = null;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_producer != null)
        {
            await _producer.DisposeAsync();
        }

        _producer = null;
    }

    private ConnectionFactory _connectionFactory = null!;
    private IConnection _connection = null!;
    private IConsumer _consumer = null!;
    private IProducer _producer = null!;

    [Test]
    public void CanResolveMessageProducer()
    {
        MessageProducer inst = Resolve<MessageProducer>();
        Assert.IsNotNull(inst);
    }


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
            CorrelationId = "Test01",
            CreationTime = DateTime.Now,
            DurabilityMode = DurabilityMode.Durable,
        });

        await consumerTask;

        StringAssert.Contains("Test01", msgReceived);
    }

    [Test]
    public async Task NonDurableVsDurable()
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