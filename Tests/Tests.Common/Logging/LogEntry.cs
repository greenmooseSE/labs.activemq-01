namespace Tests.Common.Logging;

using Microsoft.Extensions.Logging;

public class LogEntry
{
    #region Public members

    public string Category { get; }
    public LogLevel LogLevel { get; }
    public string Text { get; }
    public int ThreadId { get; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
    public TimeSpan TimestampOffset { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var logLev = LogLevel.ToString().ToUpperInvariant();
        var logLevAbbr = logLev.Substring(0, Math.Min(4, logLev.Length));
        var categoryLen = Math.Min(15, Category.Length);
        var categoryAbbr = Category.Substring(Category.Length - categoryLen, categoryLen);
        var indent = "";
        var scopeName = "";
        if (_contextScope != null)
        {
            indent = new string(' ', _contextScope.NestedScopeIndex * 2);
            scopeName = _contextScope.ScopeName;
        }

        return
            $"[{TimestampOffset:mm\\:ss\\.fff}] {logLevAbbr} {categoryAbbr} #{ThreadId:D4} - {indent}{Text} [{scopeName}]";
    }


    public LogEntry(LoggingContextScope? contextScope,
        TimeSpan timestampOffset,
        string category,
        LogLevel logLevel,
        string text)
    {
        _contextScope = contextScope;
        TimestampOffset = timestampOffset;
        Category = category;
        LogLevel = logLevel;
        Text = text;
        ThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    #endregion

    #region Non-Public members

    private readonly LoggingContextScope? _contextScope;

    #endregion
}
