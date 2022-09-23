namespace Tests.Common;

using Microsoft.Extensions.Logging;

public abstract class UnitTest
{
    #region Non-Public members

    private void Log(string message, LogLevel logLevel)
    {
        var timestamp = DateTime.Now.ToString("hh:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] {logLevel} {message}");
    }


    protected virtual void LogDebug(string message)
    {
        Log(message, LogLevel.Debug);
    }
    protected virtual void LogInfo(string message)
    {
        Log(message, LogLevel.Information);
    }

    #endregion
}
