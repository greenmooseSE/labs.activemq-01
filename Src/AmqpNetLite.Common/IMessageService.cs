﻿namespace AmqpNetLite.Common;

public interface IMessageService : IDisposable
{
    #region Public members

    Task ProcessMessagesAsync(CancellationToken stoppingToken);
    void SetQueueNames(IReadOnlyCollection<string> queueNames);

    #endregion
}
