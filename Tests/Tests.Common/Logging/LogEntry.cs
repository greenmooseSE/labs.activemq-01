using System;
using System.Linq;

namespace Tests.Common.Logging;

using Microsoft.Extensions.Logging;

public class LogEntry
{
    #region Public members

    public string Category { get; }
    public LogLevel LogLevel { get; }
    public string Text { get; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
    public TimeSpan TimestampOffset { get; }
    public int ThreadId { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var logLev = LogLevel.ToString().ToUpperInvariant();
        var logLevAbbr = logLev.Substring(0, Math.Min(4, logLev.Length));
        var categoryLen = Math.Min(15, Category.Length);
        var categoryAbbr = Category.Substring(Category.Length - categoryLen, categoryLen);
        return $"[{TimestampOffset:mm\\:ss\\.fff}] {logLevAbbr} {categoryAbbr} #{ThreadId:D4} - {Text}";
    }


    public LogEntry(TimeSpan timestampOffset, string category, LogLevel logLevel, string text)
    {
        TimestampOffset = timestampOffset;
        Category = category;
        LogLevel = logLevel;
        Text = text;
        ThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    #endregion
}
