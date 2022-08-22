namespace Tests.Common.Logging;

using Microsoft.Extensions.Logging;

public class MemoryLoggingProvider : ILoggerProvider
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #endregion

    #region ILoggerProvider members

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        // Console.WriteLine($"Creating logger for category {categoryName}.");
        var logger = new MemoryLogger(categoryName, MemoryLoggerManager.Instance);
        return logger;
    }

    #endregion
}

internal class MemoryLogger : ILogger
{
    #region ILogger members

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
    {
        return new LoggingScopeFake();
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // var text = formatter(state, exception);
        _memoryLoggerManager.Log(_category, logLevel, eventId, state, exception, formatter);
    }

    #endregion

    #region Public members

    public MemoryLogger(string category, MemoryLoggerManager memoryLoggerManager)
    {
        _category = category;
        _memoryLoggerManager = memoryLoggerManager;
    }

    #endregion

    #region Non-Public members

    private readonly string _category;
    private readonly MemoryLoggerManager _memoryLoggerManager;

    #endregion
}

internal class LoggingScopeFake : IDisposable
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #endregion
}
