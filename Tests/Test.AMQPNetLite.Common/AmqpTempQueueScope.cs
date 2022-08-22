namespace Test.AMQPNetLite.Common;

using System.Diagnostics.CodeAnalysis;
using ActiveMQ.Artemis.Client;
using Amqp;
using global::Common.EnsureExtension;

public class AmqpTempQueueScope : IDisposable
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
        Func<Task> disposeCb = async () =>
        {
            var queueWasDeleted = false;
            try
            {
                await AmqpReceiverLink.CloseAsync();
            }
            catch (AmqpException ex)
            {
                if (ex.Message.Contains("Queue was deleted"))
                {
                    queueWasDeleted = true;
                }
            }

            await AmqpSenderLink.CloseAsync();
            await AmqpReceiverLink.CloseAsync();

            //wipe all addresses
            // if (false)
            // {
            //     IReadOnlyList<string> addresses =
            //         (await topologyManager.GetQueueNamesAsync()) ?? Array.Empty<string>();
            //
            //     foreach (var address in addresses.Where(a => a.StartsWith(_addressPrefix)))
            //     {
            //         //To avoid error AMQ229205: Address AutoTestQueue-7452c193-32f1-4853-9812-fad11ef6bc00 has bindings
            //         try
            //         {
            //             // await topologyManager.DeleteQueueAsync(_topicName, removeConsumers:true, autoDeleteAddress:true);
            //             await topologyManager.DeleteAddressAsync(address, true);
            //         }
            //         catch (Exception e)
            //         {
            //             LogDebug($"Swallowing exception '{e.Message}'.");
            //         }
            //     }
            //
            // }

            await using ITopologyManager? topologyManager = await ArtemisConnectionSingleton.Instance
                .ArtemisConnection.CreateTopologyManagerAsync();

            if (!queueWasDeleted)
            {
                await topologyManager.DeleteAddressAsync(TopicName, true);
            }
        };
        disposeCb().Wait();
    }

    #endregion

    #region Public members

    public Address Address => AmqpNetLiteConnectionSingleton.Address.EnsureNotNull();

    public Connection AmqpNetLiteConnection => AmqpNetLiteConnectionSingleton.Instance.AmqpNetLiteConnection;

    public ReceiverLink AmqpReceiverLink { get; }

    public SenderLink AmqpSenderLink { get; }

    public Session AmqpSession { get; }

    public string TopicName { get; }

    //Cannot be async since awaiting it in a test setup will continue to a test teardown immediately
    public static AmqpTempQueueScope Create(string queuePrefix)
    {
        Func<Task<AmqpTempQueueScope>> instCb = async () =>
        {
            var topicName = $"{queuePrefix}{DateTime.Now.ToString("o")}-{Guid.NewGuid()}".Replace(":", "-")
                .Replace("+", "-");


            // IConnection artemisConnection = ArtemisConnectionSingleton.Instance.ArtemisConnection;
            // await using ITopologyManager artemisTopologyMgr =
            //     (await artemisConnection.CreateTopologyManagerAsync()).EnsureNotNull();

            // var address = artemisConnection.Endpoint.ToString();

            // await using IProducer? artemisProducer = await artemisConnection.CreateProducerAsync(
            //     new ProducerConfiguration
            //     {
            //         MessageDurabilityMode = DurabilityMode.Nondurable,
            //         Address = topicName,
            //         RoutingType = RoutingType.Anycast
            //     });
            //
            // await artemisTopologyMgr.CreateQueueAsync(new QueueConfiguration
            // {
            //     Address = topicName,
            //     Name = $"Queue01-{topicName}",
            //     // RoutingType = RoutingType.Anycast,
            //     Durable = false,
            //     AutoDelete = true
            // });

            var inst = new AmqpTempQueueScope(topicName);
            return inst;
        };

        AmqpTempQueueScope inst = instCb().Result;
        return inst;
    }

    #endregion

    #region Non-Public members

    ///<summary>One-time only construction logic.</summary>
    [ExcludeFromCodeCoverage]
    static AmqpTempQueueScope()
    {
        ArtemisConnectionSingleton.Configure();
        AmqpNetLiteConnectionSingleton.Configure();
    }

    private AmqpTempQueueScope(string topicName)
    {
        TopicName = topicName;
        AmqpSession = new Session(AmqpNetLiteConnectionSingleton.Instance.AmqpNetLiteConnection);
        AmqpReceiverLink = new ReceiverLink(AmqpSession, "test-receiver01", TopicName);
        AmqpSenderLink = new SenderLink(AmqpSession, "test-sender01", TopicName);
    }

    #endregion
}
