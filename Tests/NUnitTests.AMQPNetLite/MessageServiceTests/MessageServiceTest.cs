namespace NUnitTests.Misc.MessageServiceTests;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AmqpNetLite.Common;
using Common.EnsureExtension;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Test.AMQPNetLite.Common;
using Tests.Common.Logging;

[TestFixture]
internal abstract class MessageServiceTest : AmqpNetLiteTest, IIocTest
{
    private static readonly IServiceProvider _serviceProvider;
    private LoggerScope _loggerScope;
    private IMessageService? _messageServiceInstance;


    [SetUp]
    public void MessageServiceTestSetUp()
    {
        _loggerScope = MemoryLoggerManager.Instance.NewLogScope();
        
        MessageService.IterationDelay = TimeSpan.FromMilliseconds(10);
    }
    private bool DoLog => true;

    [TearDown]
    public void MessageServiceTestTearDown()
    {
        _messageServiceInstance?.Dispose();
        _messageServiceInstance = null;
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
    /// <inheritdoc />
    public T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    protected MessageService NewMessageServiceInstance()
    {
        _messageServiceInstance.EnsureNull();
        _messageServiceInstance = GetRequiredService<IMessageService>();
        return (MessageService)_messageServiceInstance;
    }

    protected void ActByCallingProcessMessages(MessageService sut, string queueName = "test-queue01")
    {
        ActByCallingProcessMessages(sut, new[]{queueName});
    }
    protected void ActByCallingProcessMessages(MessageService sut, IReadOnlyCollection<string> queueNames)
    {
        sut.SetQueueNames(queueNames);
        var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        sut.ProcessMessagesAsync(token.Token).Wait();

    }
}

internal interface IIocTest
{
    T GetRequiredService<T>();
    

}
