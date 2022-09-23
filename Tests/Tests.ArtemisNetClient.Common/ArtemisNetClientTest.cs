namespace Tests.ArtemisNetClient.Common;

using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using ActiveMqLabs01.Common;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Common;

[TestFixture]
internal class ArtemisNetClientTest : UnitTest
{

    ///<summary>One-time only construction logic.</summary>
    // [ExcludeFromCodeCoverage]
    static ArtemisNetClientTest()
    {
        var services = new ServiceCollection();
        Endpoint[] endpoints = {Endpoint.Create("localhost", 5672, "admin", "admin")};
        IActiveMqBuilder? activeMqBuilder = services.AddActiveMq("bookstore-cluster", endpoints);
        services.AddActiveMqHostedService();
        activeMqBuilder.AddAnonymousProducer<MessageProducer>();


        _serviceProvider = activeMqBuilder.Services.BuildServiceProvider();
    }

    private static readonly IServiceProvider _serviceProvider;

    protected T Resolve<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}
