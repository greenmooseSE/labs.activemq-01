namespace Tests.Common
{
    using System;
    using System.Linq;
    using ActiveMQ.Artemis.Client;
    using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
    using ActiveMQ.Artemis.Client.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class UnitTest
    {
        private static readonly ServiceProvider _serviceProvider;

        ///<summary>One-time only construction logic.</summary>
        // [ExcludeFromCodeCoverage]
        static UnitTest()
        {
            var services = new ServiceCollection();
            var endpoints = new[] {Endpoint.Create(host: "localhost", port: 5672, "admin", "admin")};
            services.AddActiveMq("bookstore-cluster", endpoints);
            services.AddActiveMqHostedService();

            _serviceProvider = services.BuildServiceProvider();
        }

        private IServiceProvider ServiceProvider => _serviceProvider;

        protected T Resolve<T>()
        {
            string moo = "hej";
            moo = null;

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}