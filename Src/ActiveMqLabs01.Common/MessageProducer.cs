namespace ActiveMqLabs01.Common;

using System;
using System.Linq;
using System.Text.Json;
using ActiveMQ.Artemis.Client;

public class MessageProducer
{
    private readonly IAnonymousProducer _producer;

    public MessageProducer(IAnonymousProducer producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync<T>(T message)
    {
        var serialized = JsonSerializer.Serialize(message);
        var address = typeof(T).Name;
        var msg = new Message(serialized);
        await _producer.SendAsync(address, msg);
    }
}
