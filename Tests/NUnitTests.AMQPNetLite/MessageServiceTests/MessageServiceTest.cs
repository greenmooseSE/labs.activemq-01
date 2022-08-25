namespace NUnitTests.Misc.MessageServiceTests;

using AmqpNetLite.Common;
using Common.EnsureExtension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.AMQPNetLite.Common;
using Tests.Common.Logging;

[TestFixture]
internal abstract class MessageServiceTest : AmqpNetLiteTest, IIocTest
{
    [SetUp]
    public void MessageServiceTestSetUp()
    {
        _loggerScope = MemoryLoggerManager.Instance.NewLogScope();

        MessageService.IterationDelay = TimeSpan.FromMilliseconds(10);
        _logger = new MemoryLogger(GetType().Name, MemoryLoggerManager.Instance);
        ActCancellationTokenParamCb = null;
    }

    [TearDown]
    public void MessageServiceTestTearDown()
    {
        DisposeCurrentMessageServiceInstance();
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
        _logger = null;
    }

    ///<summary>One-time only construction logic.</summary>
    static MessageServiceTest()
    {
        var iocHelper = new IocHelper();

        iocHelper.Services.AddTransient<IMessageService, MessageService>();

        _serviceProvider = iocHelper.BuildServiceProvider();
    }

    private MemoryLogger? _logger;

    private LoggerScope _loggerScope;
    private IMessageService? _messageServiceInstance;
    private static readonly IServiceProvider _serviceProvider;
    protected Func<CancellationTokenSource>? ActCancellationTokenParamCb = null;

    protected void ActByCallingProcessMessages(MessageService sut,
        string queueName = "test-queue01",
        int creditLimit = 1,
        string actLogScopeName="Act")
    {
        using var scope = Logger.BeginScope(actLogScopeName);
        Logger.LogInformation("ActByCallingProcessMessages({queueName})", queueName);
        ActByCallingProcessMessages(sut, new[] {queueName}, creditLimit);
    }

    protected void ActByCallingProcessMessages(MessageService sut,
        IReadOnlyCollection<string> queueNames,
        int creditLimit = 1)
    {
        sut.SetQueueNames(queueNames, creditLimit);
        ActByCallingProcessMessagesKeepCurrentQueues(sut);
    }

    

    protected void ActByCallingProcessMessagesKeepCurrentQueues(MessageService sut)
    {
        CancellationTokenSource token;
        if (ActCancellationTokenParamCb == null)
            token = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        else
            token = ActCancellationTokenParamCb();
        sut.ProcessMessagesAsync(token.Token).Wait();
    }

    /// <summary>Disposes the instance created via <see cref="NewMessageServiceInstance" />.</summary>
    protected void DisposeCurrentMessageServiceInstance()
    {
        _messageServiceInstance?.Dispose();
        _messageServiceInstance = null;
    }

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc />
    protected override void LogDebug(string message)
    {
        Logger.LogDebug(message);
    }

    /// <inheritdoc />
    protected override void LogInfo(string message)
    {
        Logger.LogInformation(message);
    }

    /// <summary>
    ///     Creates a new instance. If multiple instances are to be created in same tests you must call
    ///     <see cref="DisposeCurrentMessageServiceInstance" /> between creations.
    /// </summary>
    protected MessageService NewMessageServiceInstance()
    {
        _messageServiceInstance.EnsureNull();
        _messageServiceInstance = NewMessageServiceInstanceNoAutoDispose();
        return (MessageService)_messageServiceInstance;
    }

    /// <summary>
    ///     Creates a new instance without disposing it during tear down. Caller is responsible for the returned instance
    ///     to be disposed.
    ///     Credit is default set to 1.
    /// </summary>
    protected MessageService NewMessageServiceInstanceNoAutoDispose()
    {
        var msgService = (MessageService)GetRequiredService<IMessageService>();
        return msgService;
    }

    private bool DoLog => true;

    protected ILogger Logger => _logger.EnsureNotNull();
}

internal interface IIocTest
{
    #region Public members

    T GetRequiredService<T>();

    #endregion
}
