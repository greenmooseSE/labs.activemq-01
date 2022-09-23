namespace AmqpNetLite.Common;

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
        _log.LogDebug("Disposing instance {id}.", _uniqueId);
        _connection.Close();
    }

    #endregion

    #region IMessageService members

    /// <inheritdoc />
    public async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using IDisposable s1 = _log.BeginScope(nameof(ProcessMessagesAsync) + $"-{_uniqueId}");
        // var host = new ContainerHost(_address);
        var serviceName = nameof(MessageService);


        Task<MessageInfo> ReceiveMsgTask(ReceiverLinkWrapper receiverLinkWrapper)
        {
            return Task.Factory.StartNew(async () =>
                {
                    ReceiverLink receiverLink = receiverLinkWrapper.ReceiverLink;
                    _log.LogDebug("Starting to listening in receiver {receiverName}", receiverLink.Name);
                    Message? msg = await receiverLink.ReceiveAsync(IterationDelay);
                    _log.LogDebug(
                        "Stopped listening in receiver {receiverName} (DidGetMessage: {DidGetMessage})",
                        receiverLink.Name,
                        msg != null);

                    return new MessageInfo(receiverLinkWrapper, msg);
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
            foreach (ReceiverLinkWrapper receiverLink in _receiverLinks)
            {
                Task<MessageInfo> receiveMsgTask = ReceiveMsgTask(receiverLink);
                receiveMsgTaskMap.TryAdd(receiverLink.ReceiverLink.Name, receiveMsgTask).EnsureTrue();
            }

            //While we have pending tasks, keep listening to messages until we should cancel
            while (receiveMsgTaskMap.Count > 0)
            {
                Task<MessageInfo>[] allTasks = receiveMsgTaskMap.Values.ToArray();
                var completedTaskIdx = Task.WaitAny(allTasks);
                _log.LogDebug("Removing completed task at idx {idx}", completedTaskIdx);
                using IDisposable s2 = _log.BeginScope($"Task-{completedTaskIdx}");
                MessageInfo receivedMsgInfo = allTasks[completedTaskIdx].Result;

                ReceiverLinkWrapper receiverLinkWrapper = receivedMsgInfo.ReceiverLink;
                ReceiverLink receiverLink = receiverLinkWrapper.ReceiverLink;

                Message? msg = receivedMsgInfo.Message;
                if (msg != null)
                {
                    _log.LogTrace("Firing events for message in receiver link {receiverName}.",
                        receiverLink.Name);
                    {
                        using IDisposable s3 = _log.BeginScope("OnProcessMessage");
                        _log.LogTrace("Invoking OnProcessMessage");
                        OnProcessMessage?.Invoke(msg);
                    }
                    {
                        using IDisposable s4 = _log.BeginScope("OnProcessMessageEx");
                        _log.LogTrace("Invoking OnProcessMessageEx");
                        OnProcessMessageEx?.Invoke(msg, receiverLink);
                    }
                }

                receiveMsgTaskMap.TryRemove(receiverLink.Name, out _).EnsureTrue();
                if (!stoppingToken.IsCancellationRequested)
                {
                    _log.LogDebug("Sleeping {duration} after receiver link {receiverName} fetch.",
                        SleepPeriodAfterFetchedMessage,
                        receiverLink.Name);
                    await Task.Delay(SleepPeriodAfterFetchedMessage);
                    receiveMsgTaskMap.TryAdd(receiverLink.Name, ReceiveMsgTask(receiverLinkWrapper))
                        .EnsureTrue();
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
        SetQueueNamesHelper(queueNames);
    }

    public void SetQueueNames(IReadOnlyCollection<string> queueNames, int creditLimit)
    {
        SetQueueNamesHelper(queueNames, creditLimit);
    }

    #endregion

    #region Public members

    public static TimeSpan IterationDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public static TimeSpan SleepPeriodAfterFetchedMessage { get; } = TimeSpan.FromMilliseconds(10);

    public event Action<Message>? OnProcessMessage;
    public event Action<Message, ReceiverLink>? OnProcessMessageEx;

    public void CloseReceiverLinks()
    {
        foreach (ReceiverLinkWrapper receiverLink in _receiverLinks)
        {
            receiverLink.ReceiverLink.Close();
        }
    }

    public void CloseSession()
    {
        _session.Close();
    }

    public void SetCreditLimit(int creditLimit)
    {
        foreach (ReceiverLinkWrapper receiverLink in _receiverLinks)
        {
            SetCredit(receiverLink, creditLimit);
        }
    }

    private long _uniqueId = 0;
    public void SetQueueNamesHelper(IReadOnlyCollection<string> queueNames, int? creditLimit = null)
    {
        _receiverLinks.EnsureEmpty();

        var queueCount = queueNames.Select((queueName, idx) =>
            {
                var receiverLinkWrapper = new ReceiverLinkWrapper(new ReceiverLink(_session,
                    $"receiver-link-{idx + 1}-{Interlocked.Increment(ref _uniqueId)}",
                    queueName));
                if (creditLimit.HasValue)
                {
                    SetCredit(receiverLinkWrapper, creditLimit.Value);
                }

                _receiverLinks.Add(receiverLinkWrapper);
                return idx;
            })
            .ToList()
            .Count;
        _log.LogDebug("Setting {queueCount} no of queue names.", queueCount);
    }

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
    private readonly List<ReceiverLinkWrapper> _receiverLinks = new();
    private readonly Session _session;

    private void SetCredit(ReceiverLinkWrapper receiverLink, int creditLimit)
    {
        //If we use CreditMode.Manual, the receiver link will not automatically get new messages 
        //  If we use CreditMode.Auto, it seems
        receiverLink.SetCredit(creditLimit, CreditMode.Manual);
    }

    #region Nested types

    private class MessageInfo
    {
        #region Public members

        public Message? Message { get; }
        public ReceiverLinkWrapper ReceiverLink { get; }

        public MessageInfo(ReceiverLinkWrapper receiverLink, Message? message)
        {
            ReceiverLink = receiverLink;
            Message = message;
        }

        #endregion
    }

    #endregion

    #endregion
}
