using System;
using System.Linq;

namespace AmqpNetLite.Common
{
    public interface IMessageService
    {
        Task ProcessMessagesAsync(CancellationToken stoppingToken);
    }
}
