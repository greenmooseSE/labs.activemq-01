using System;
using System.Linq;

namespace AmqpNetLite.Common;

using System;
using System.Linq;
using Amqp;
using Amqp.Listener;
using Microsoft.Extensions.Logging;

public class MessageService : IMessageService
{
    private readonly ILogger<MessageService> _log;
    private readonly Connection _connection;
    private readonly List<string> _queueNames;
    private readonly Address _address;

    public MessageService(ILogger<MessageService> log)
    {
        _log = log;
        string host = "localhost";
        int port = 5672;
        string user = "admin";
        string pwd = "admin";
        _address = new Address($"amqp://{user}:{pwd}@{host}:{port}");

        // _connection = new Connection(address);

       var noOfQueues = 10;
        _queueNames = Enumerable.Range(0, noOfQueues).Select(idx => $"Queue{idx:4,D4}").ToList();

    }
    public static TimeSpan IterationDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc />
    public async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        // var host = new ContainerHost(_address);

        while (!stoppingToken.IsCancellationRequested)
        {
            
            // OnIteration?.Invoke();
            // _log.LogDebug("{serviceName} is doing work with iteration delay {IterationDelay}.", serviceName, IterationDelay);
            // _messageService.StatisticsServiceDoingWork();
            await Task.Delay(IterationDelay, stoppingToken);
        }

    }
}


