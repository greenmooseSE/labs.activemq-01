using System;
using System.Linq;

namespace Tests.Common.Logging;

using global::Common.EnsureExtension;
using Microsoft.Extensions.Logging;

public class MemoryLoggerManager
{
    #region Public members

    public static MemoryLoggerManager Instance { get; } = new();

    // public void Log(string category, LogLevel logLevel, string text)
    // {
    //     // Console.WriteLine($"Logger {_scopeId}: {text}");
    //     // if (_scopeId.HasValue)
    //     // {
    //     //     var logScope = _logEntries.GetOrAdd(_scopeId.EnsureNotNull(), _=>new LoggerScope(this));
    //     //     logScope.AddLog(new LogEntry(category, logLevel, text));
    //     // }
    //     // else
    //     // {
    //     //     Console.WriteLine($"WARNING: No scope id set for logger text: {text}");
    //     }
    // }

    // public IReadOnlyList<LogEntry> GetLogsForCurrentScope()
    // {
    //     ConcurrentBag<LogEntry> logEntries;
    //     if (_logEntries.TryGetValue(_scopeId.EnsureNotNull(), out logEntries))
    //         return logEntries.ToList();
    //     return Array.Empty<LogEntry>();
    //
    // }

    public void Log<TState>(string category,
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        _loggerScope.AddLog(category, logLevel, eventId, state, exception, formatter);
    }

    public LoggerScope NewLogScope()
    {
        _loggerScope.EnsureNull();
        _loggerScope = new LoggerScope(this);
        // _scopeId.EnsureNull();
        // _scopeId = Guid.NewGuid();
        return _loggerScope;
    }

    public void RemoveLogScope()
    {
        if (_loggerScope == null)
        {
            Console.WriteLine("WARNING: Expected log scope to exist but it did not.");
        }
        else
        {
            _loggerScope = null;
        }
    }

    #endregion

    #region Non-Public members

    // private Guid? _scopeId;
    private MemoryLoggerManager()
    {
    }


    //
    // private static ConcurrentDictionary<Guid, LoggerScope> _logEntries =
    //     new ConcurrentDictionary<Guid, LoggerScope>();

    private LoggerScope? _loggerScope;

    #endregion
}
