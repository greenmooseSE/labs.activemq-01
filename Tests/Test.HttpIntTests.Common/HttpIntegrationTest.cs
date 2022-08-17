namespace Test.HttpIntTests.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RestApi.Common;
using RestApi.Common.EnsureExtension;
using Tests.Common;

public abstract class HttpIntegrationTest : UnitTest
{
    #region Public members

    protected HttpClient HttpClient => _httpClient.EnsureNotNull();

    [SetUp]
    public void HttpIntegrationTestSetUp()
    {
        if (_httpClient == null)
        {
            _webApiRegHelper.OnRegisterServices += OnAppRegisterServices;
            _httpClient = NewHttpClient();
        }
    }

    #endregion

    #region Non-Public members

    private static HttpClient? _httpClient;
    private static readonly WebApiRegistrationHelper _webApiRegHelper = new();

    protected abstract HttpClient NewHttpClient();

    private void OnAppRegisterServices(IServiceCollection services)
    {
        services.AddLogging(cfg =>
        {
            cfg.ClearProviders();
            cfg.SetMinimumLevel(LogLevel.Trace);
            cfg.AddSimpleConsole();
        });
    }

    #endregion
}
