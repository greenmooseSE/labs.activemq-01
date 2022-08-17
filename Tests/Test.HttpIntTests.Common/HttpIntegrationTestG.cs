namespace Test.HttpIntTests.Common;

using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

[TestFixture]
public abstract class HttpIntegrationTestG<T> : HttpIntegrationTest where T : class
{
    protected override HttpClient NewHttpClient()
    {
        WebApplicationFactory<T> application = new WebApplicationFactory<T>().WithWebHostBuilder(builder =>
        {
        });

        HttpClient client = application.CreateClient();
        return client;
    }
}
