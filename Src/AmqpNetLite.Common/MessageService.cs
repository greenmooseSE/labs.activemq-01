﻿namespace AmqpNetLite.Common;

using System.Collections.Concurrent;
using System.Diagnostics;
using Amqp;
using global::Common.EnsureExtension;
using Microsoft.Extensions.Logging;

public class MessageService : IMessageService
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Close();
    }

    #endregion

    #region IMessageService members

    /// <inheritdoc />
    public async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        // var host = new ContainerHost(_address);
        var serviceName = nameof(MessageService);


        Task<MessageInfo> ReceiveMsgTask(ReceiverLink receiverLink)
        {
            return Task.Factory.StartNew(async () =>
                {
                    _log.LogDebug("Starting to listening in receiver {receiverName}", receiverLink.Name);
                    Message? msg = await receiverLink.ReceiveAsync(IterationDelay);
                    _log.LogDebug("Stopped in receiver {receiverName}", receiverLink.Name);
                    return new MessageInfo(receiverLink, msg);
                })
                .Unwrap();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // OnIteration?.Invoke();
            _log.LogDebug("{serviceName} is doing work with iteration delay {IterationDelay}.",
                serviceName,
                IterationDelay);
            // _messageService.StatisticsServiceDoingWork();
            var sw = Stopwatch.StartNew();

            //Create the tasks to receive msgs
            var receiveMsgTaskMap = new ConcurrentDictionary<string, Task<MessageInfo>>();
            foreach (ReceiverLink receiverLink in _receiverLinks)
            {
                Task<MessageInfo> receiveMsgTask = ReceiveMsgTask(receiverLink);
                receiveMsgTaskMap.TryAdd(receiverLink.Name, receiveMsgTask).EnsureTrue();
            }

            //While we have pending tasks, keep listening to messages until we should cancel
            while (receiveMsgTaskMap.Count > 0)
            {
                Task<MessageInfo>[] allTasks = receiveMsgTaskMap.Values.ToArray();
                var completedTaskIdx = Task.WaitAny(allTasks);
                _log.LogDebug("Removing completed task at idx {idx}", completedTaskIdx);
                MessageInfo receivedMsgInfo = allTasks[completedTaskIdx].Result;

                ReceiverLink receiverLink = receivedMsgInfo.ReceiverLink;
                Message? msg = receivedMsgInfo.Message;
                if (msg != null)
                {
                    _log.LogDebug("Got message in receiver link {receiverName}.", receiverLink.Name);
                    OnProcessMessage?.Invoke(msg);
                }

                receiveMsgTaskMap.TryRemove(receiverLink.Name, out _).EnsureTrue();
                if (!stoppingToken.IsCancellationRequested)
                {
                    _log.LogDebug("Sleeping {duration} after receiver link {receiverName} fetch.",
                        SleepPeriodAfterFetchedMessage,
                        receiverLink.Name);
                    await Task.Delay(SleepPeriodAfterFetchedMessage);
                    receiveMsgTaskMap.TryAdd(receiverLink.Name, ReceiveMsgTask(receiverLink)).EnsureTrue();
                }
            }


            TimeSpan timeLeft = sw.Elapsed - IterationDelay;
            try
            {
                if (timeLeft < TimeSpan.Zero)
                {
                    await Task.Delay(IterationDelay, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                //swallow since we bail out during while condition
            }
        }
    }

    public void SetQueueNames(IReadOnlyCollection<string> queueNames)
    {
        _receiverLinks.EnsureEmpty();

        var queueCount = queueNames.Select((queueName, idx) =>
            {
                var receiverLink = new ReceiverLink(_session, $"receiver-link-{idx + 1}", queueName);
                _receiverLinks.Add(receiverLink);
                return idx;
            })
            .ToList()
            .Count;
        _log.LogDebug("Setting {queueCount} no of queue names.", queueCount);
    }

    #endregion

    #region Public members

    public static TimeSpan IterationDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public static TimeSpan SleepPeriodAfterFetchedMessage { get; } = TimeSpan.FromMilliseconds(10);

    public event Action<Message>? OnProcessMessage;

    public MessageService(ILogger<MessageService> log)
    {
        _log = log;
        var host = "localhost";
        var port = 5672;
        var user = "admin";
        var pwd = "admin";
        _address = new Address($"amqp://{user}:{pwd}@{host}:{port}");
        _connection = new Connection(_address);
        _session = new Session(_connection);
    }

    #endregion

    #region Non-Public members

    private readonly Address _address;
    private readonly Connection _connection;
    private readonly ILogger<MessageService> _log;
    private readonly List<ReceiverLink> _receiverLinks = new();
    private readonly Session _session;

    #region Nested types

    private class MessageInfo
    {
        #region Public members

        public Message? Message { get; }
        public ReceiverLink ReceiverLink { get; }

        public MessageInfo(ReceiverLink receiverLink, Message? message)
        {
            ReceiverLink = receiverLink;
            Message = message;
        }

        #endregion
    }

    #endregion

    #endregion
}
