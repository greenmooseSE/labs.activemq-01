namespace Tests.Common
{
    using System;
    using System.Linq;
    using ActiveMQ.Artemis.Client;
    using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
    using ActiveMQ.Artemis.Client.Extensions.Hosting;
    using ActiveMqLabs01.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public abstract class UnitTest
    {
        private static readonly IServiceProvider _serviceProvider;

        ///<summary>One-time only construction logic.</summary>
        // [ExcludeFromCodeCoverage]
        static UnitTest()
        {
            var services = new ServiceCollection();
            var endpoints = new[] {Endpoint.Create(host: "localhost", port: 5672, "admin", "admin")};
            var activeMqBuilder = services.AddActiveMq("bookstore-cluster", endpoints);
            services.AddActiveMqHostedService();
            activeMqBuilder.AddAnonymousProducer<MessageProducer>();
            
            

            _serviceProvider = activeMqBuilder.Services.BuildServiceProvider();
        }


        protected T Resolve<T>() where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }

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
