namespace Tests.Common.Logging;

using Microsoft.Extensions.Logging;

/// <summary>Represents a scope created via <see cref="ILogger.BeginScope{TState}" /></summary>
public class LoggingContextScope
{
    #region Public members

    public MemoryLogger Logger { get; }
    public int NestedScopeIndex { get; }
    public string ScopeName { get; }

    public LoggingContextScope(int nestedScopeIndex, string scopeName, MemoryLogger logger)
    {
        NestedScopeIndex = nestedScopeIndex;
        ScopeName = scopeName;
        Logger = logger;
    }

    #endregion
}
