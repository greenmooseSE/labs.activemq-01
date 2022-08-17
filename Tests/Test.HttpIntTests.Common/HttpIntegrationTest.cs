namespace Test.HttpIntTests.Common;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RestApi.Common;
using RestApi.Common.EnsureExtension;
using Tests.Common;

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

internal class MemoryLoggingProvider : ILoggerProvider
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #endregion

    #region ILoggerProvider members

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        // Console.WriteLine($"Creating logger for category {categoryName}.");
        var logger = new MemoryLogger(categoryName, MemoryLoggerManager.Instance);
        return logger;
    }

    #endregion
}

internal class LoggerScope : IDisposable
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
        _parent.RemoveLogScope();
    }

    #endregion

    #region Public members

    public IReadOnlyList<LogEntry> AllLogs => _logEntries.OrderBy(l => l.TimestampOffset).ToList();


    public void AddLog<TState>(string category,
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var logEntry = new LogEntry(DateTimeOffset.Now - _started,
            category,
            logLevel,
            formatter(state, exception));
        _logEntries.Add(logEntry);
    }

    public LoggerScope(MemoryLoggerManager parent)
    {
        _parent = parent;
    }

    #endregion

    #region Non-Public members

    //private readonly Guid _scopeId;

    private readonly ConcurrentBag<LogEntry> _logEntries = new();
    private readonly MemoryLoggerManager _parent;
    private readonly DateTimeOffset _started = DateTimeOffset.Now;

    #endregion
}

internal class LogEntry
{
    #region Public members

    public string Category { get; }
    public LogLevel LogLevel { get; }
    public string Text { get; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
    public TimeSpan TimestampOffset { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var logLev = LogLevel.ToString().ToUpperInvariant();
        var logLevAbbr = logLev.Substring(0, Math.Min(4, logLev.Length));
        var categoryLen = Math.Min(15, Category.Length);
        var categoryAbbr = Category.Substring(Category.Length - categoryLen, categoryLen);
        return $"[{TimestampOffset:mm\\:ss\\.fff}] {logLevAbbr} {categoryAbbr} - {Text}";
    }


    public LogEntry(TimeSpan timestampOffset, string category, LogLevel logLevel, string text)
    {
        TimestampOffset = timestampOffset;
        Category = category;
        LogLevel = logLevel;
        Text = text;
    }

    #endregion
}

internal class MemoryLoggerManager
{
    #region Public members

    public static MemoryLoggerManager Instance { get; } = new();

    // public void Log(string category, LogLevel logLevel, string text)
    // {
    //     // Console.WriteLine($"Logger {_scopeId}: {text}");
    //     // if (_scopeId.HasValue)
    //     // {
    //     //     var logScope = _logEntries.GetOrAdd(_scopeId.EnsureNotNull(), _=>new LoggerScope(this));
    //     //     logScope.AddLog(new LogEntry(category, logLevel, text));
    //     // }
    //     // else
    //     // {
    //     //     Console.WriteLine($"WARNING: No scope id set for logger text: {text}");
    //     }
    // }

    // public IReadOnlyList<LogEntry> GetLogsForCurrentScope()
    // {
    //     ConcurrentBag<LogEntry> logEntries;
    //     if (_logEntries.TryGetValue(_scopeId.EnsureNotNull(), out logEntries))
    //         return logEntries.ToList();
    //     return Array.Empty<LogEntry>();
    //
    // }

    public void Log<TState>(string category,
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        _loggerScope.AddLog(category, logLevel, eventId, state, exception, formatter);
    }

    public LoggerScope NewLogScope()
    {
        _loggerScope.EnsureNull();
        _loggerScope = new LoggerScope(this);
        // _scopeId.EnsureNull();
        // _scopeId = Guid.NewGuid();
        return _loggerScope;
    }

    public void RemoveLogScope()
    {
        if (_loggerScope == null)
        {
            Console.WriteLine("WARNING: Expected log scope to exist but it did not.");
        }
        else
        {
            _loggerScope = null;
        }
    }

    #endregion

    #region Non-Public members

    // private Guid? _scopeId;
    private MemoryLoggerManager()
    {
    }


    //
    // private static ConcurrentDictionary<Guid, LoggerScope> _logEntries =
    //     new ConcurrentDictionary<Guid, LoggerScope>();

    private LoggerScope? _loggerScope;

    #endregion
}

internal class MemoryLogger : ILogger
{
    #region ILogger members

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
    {
        return new LoggingScopeFake();
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // var text = formatter(state, exception);
        _memoryLoggerManager.Log(_category, logLevel, eventId, state, exception, formatter);
    }

    #endregion

    #region Public members

    public MemoryLogger(string category, MemoryLoggerManager memoryLoggerManager)
    {
        _category = category;
        _memoryLoggerManager = memoryLoggerManager;
    }

    #endregion

    #region Non-Public members

    private readonly string _category;
    private readonly MemoryLoggerManager _memoryLoggerManager;

    #endregion
}

internal class LoggingScopeFake : IDisposable
{
    #region IDisposable members

    /// <inheritdoc />
    public void Dispose()
    {
    }

    #endregion
}
