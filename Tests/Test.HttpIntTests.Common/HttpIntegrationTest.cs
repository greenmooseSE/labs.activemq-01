namespace Test.HttpIntTests.Common;

using global::Common.EnsureExtension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RestApi.Common;
using Tests.Common;
using Tests.Common.Logging;

public abstract class HttpIntegrationTest : UnitTest
{
    #region Public members

    public static IServiceProvider AppServiceProvider => _appServiceProvider.EnsureNotNull();

    private bool DoLog => true;

    protected virtual bool DoRegisterWebApiHostServices { get; } = false;

    protected HttpClient HttpClient => _httpClient.EnsureNotNull();

    [SetUp]
    public void HttpIntegrationTestSetUp()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            _webApiRegHelper = (WebApiRegistrationHelper)WebApiRegistrationHelper.Instance;
            if (DoRegisterWebApiHostServices)
            {
                _webApiRegHelper.DoRegisterHostedServices = true;
            }
            else
            {
                _webApiRegHelper.DoRegisterHostedServices = false;
            }
        }

        _loggerScope = MemoryLoggerManager.Instance.NewLogScope();

        if (_httpClient == null)
        {
            _webApiRegHelper.OnRegisterServices += OnAppRegisterServices;
            _webApiRegHelper.OnRegisterServiceProvider += OnAppRegisterServiceProvider;
            _httpClient = NewHttpClient();
        }
    }

    [TearDown]
    public void HttpIntegrationTestTearDown()
    {
        if (DoLog)
        {
            IReadOnlyList<LogEntry> logs = _loggerScope.EnsureNotNull().AllLogs;
            foreach (LogEntry log in logs)
            {
                Console.WriteLine(log);
            }
        }

        _loggerScope?.Dispose();
        _loggerScope = null;
    }

    #endregion

    #region Non-Public members

    private static IServiceProvider? _appServiceProvider;

    private static HttpClient? _httpClient;
    private static bool _isInitialized;

    private LoggerScope? _loggerScope;
    private static WebApiRegistrationHelper? _webApiRegHelper;

    /// <inheritdoc />
    protected override void LogDebug(string message)
    {
        ILogger<HttpIntegrationTest> logger =
            AppServiceProvider.GetRequiredService<ILogger<HttpIntegrationTest>>();
        logger.LogDebug(message);
    }

    /// <inheritdoc />
    protected override void LogInfo(string message)
    {
        ILogger<HttpIntegrationTest> logger =
            AppServiceProvider.GetRequiredService<ILogger<HttpIntegrationTest>>();
        logger.LogInformation(message);
    }


    protected abstract HttpClient NewHttpClient();

    private void OnAppRegisterServiceProvider(IServiceProvider serviceProvider)
    {
        _appServiceProvider.EnsureNull();
        _appServiceProvider = serviceProvider;
    }

    protected virtual void OnAppRegisterServices(IServiceCollection services)
    {
        services.AddLogging(cfg =>
        {
            cfg.ClearProviders();
            cfg.AddFilter(_ =>
            {
                return true;
            });
            cfg.AddProvider(new MemoryLoggingProvider());
            // cfg.AddSimpleConsole();
        });
    }

    #endregion
}
