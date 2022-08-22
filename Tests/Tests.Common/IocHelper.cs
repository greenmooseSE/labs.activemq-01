using System;
using System.Linq;

namespace NUnitTests.Misc.MessageServiceTests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Common.Logging;

public class IocHelper
{
    readonly ServiceCollection _services = new();

    public ServiceCollection Services => _services;

    public IocHelper()
    {
        _services.AddLogging(cfg =>
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

    public IServiceProvider BuildServiceProvider()
    {
        var res = Services.BuildServiceProvider();
        return res;
    }
}
