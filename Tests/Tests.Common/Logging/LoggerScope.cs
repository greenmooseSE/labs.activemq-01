using System;
using System.Linq;

namespace Tests.Common.Logging;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public class LoggerScope : IDisposable
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
        _parent.RemoveLogScope();
    }

    #endregion

    #region Public members

    public IReadOnlyList<LogEntry> AllLogs => _logEntries.OrderBy(l => l.TimestampOffset).ToList();


    public void AddLog<TState>(LoggingContextScope? loggingContextScope,
        string category,
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var logEntry = new LogEntry(loggingContextScope,
            DateTimeOffset.Now - _started,
            category,
            logLevel,
            formatter(state, exception));
        _logEntries.Add(logEntry);
    }

    public LoggerScope(MemoryLoggerManager parent)
    {
        _parent = parent;
    }

    #endregion

    #region Non-Public members

    //private readonly Guid _scopeId;

    private readonly ConcurrentBag<LogEntry> _logEntries = new();
    private readonly MemoryLoggerManager _parent;
    private readonly DateTimeOffset _started = DateTimeOffset.Now;

    #endregion
}
