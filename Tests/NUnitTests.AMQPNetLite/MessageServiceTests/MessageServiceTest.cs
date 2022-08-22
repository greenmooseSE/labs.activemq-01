namespace NUnitTests.Misc.MessageServiceTests;

using AmqpNetLite.Common;
using Common.EnsureExtension;
using Microsoft.Extensions.DependencyInjection;
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
    }

    ///<summary>One-time only construction logic.</summary>
    static MessageServiceTest()
    {
        var iocHelper = new IocHelper();

        iocHelper.Services.AddTransient<IMessageService, MessageService>();

        _serviceProvider = iocHelper.BuildServiceProvider();
    }

    private LoggerScope _loggerScope;
    private IMessageService? _messageServiceInstance;
    private static readonly IServiceProvider _serviceProvider;

    protected void ActByCallingProcessMessages(MessageService sut, string queueName = "test-queue01")
    {
        ActByCallingProcessMessages(sut, new[] {queueName});
    }

    protected void ActByCallingProcessMessages(MessageService sut, IReadOnlyCollection<string> queueNames)
    {
        sut.SetQueueNames(queueNames);
        ActByCallingProcessMessagesKeepCurrentQueues(sut);
    }

    protected void ActByCallingProcessMessagesKeepCurrentQueues(MessageService sut)
    {
        var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
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

    /// <summary>
    ///     Creates a new instance. If multiple instances are to be created in same tests you must call
    ///     <see cref="DisposeCurrentMessageServiceInstance" /> between creations.
    /// </summary>
    protected MessageService NewMessageServiceInstance()
    {
        _messageServiceInstance.EnsureNull();
        _messageServiceInstance = GetRequiredService<IMessageService>();
        return (MessageService)_messageServiceInstance;
    }

    /// <summary>
    ///     Creates a new instance without disposing it during tear down. Caller is responsible for the returned instance
    ///     to be disposed.
    /// </summary>
    protected MessageService NewMessageServiceInstanceNoAutoDispose()
    {
        return (MessageService)GetRequiredService<IMessageService>();
    }

    private bool DoLog => true;
}

internal interface IIocTest
{
    #region Public members

    T GetRequiredService<T>();

    #endregion
}
