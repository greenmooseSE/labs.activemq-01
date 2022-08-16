namespace Tests.Common
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public abstract class UnitTest
    {



      

        protected void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        private void Log(string message, LogLevel logLevel)
        {
            var timestamp = DateTime.Now.ToString("hh:mm:ss.fff");
            Console.WriteLine($"[{timestamp}] {logLevel} {message}");
        }
    }
}
