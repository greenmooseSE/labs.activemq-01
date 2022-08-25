namespace Tests.Common.Logging;

using System.Collections.Concurrent;
using global::Common.EnsureExtension;
using Microsoft.Extensions.Logging;

public class MemoryLoggerManager
{
    #region Public members

    public static MemoryLoggerManager Instance { get; } = new();

    public IDisposable BeginScope<TState>(TState state, MemoryLogger memoryLogger)
    {
        var newIndex = Interlocked.Increment(ref _nestedScopeIndex) ;
        // Console.WriteLine($"NEW INDEX: {newIndex}");
        var scope = new LoggingContextScope(newIndex, state.ToString() ?? "", memoryLogger);
        _scopeIndexMap.TryAdd(newIndex, scope).EnsureTrue();
        return new LoggingScopeFake(() =>
        {
            var indexRemoved = Interlocked.Decrement(ref _nestedScopeIndex)+1;
            // Console.WriteLine($"REMOVED INDEX: {indexRemoved}");
            _scopeIndexMap.TryRemove(indexRemoved, out _).EnsureTrue();
        });
    }

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
        LoggingContextScope? contextScope=null;
        _scopeIndexMap.TryGetValue(_nestedScopeIndex, out contextScope);
        _loggerScope.EnsureNotNull()
            .AddLog(contextScope, category, logLevel, eventId, state, exception, formatter);
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
    private int _nestedScopeIndex;

    private readonly ConcurrentDictionary<int, LoggingContextScope> _scopeIndexMap = new();

    #endregion
}
