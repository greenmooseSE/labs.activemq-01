using System;
using System.Linq;

namespace RestApi.Common;

using Microsoft.Extensions.DependencyInjection;
using RestApi.Common.EnsureExtension;

public class WebApiRegistrationHelper : IWebApiRegistrationHelper
{
    #region IWebApiRegistrationHelper members

    public void RegisterServices(IServiceCollection services)
    {
        OnRegisterServices?.Invoke(services);
    }

    #endregion

    #region Public members

    public static IWebApiRegistrationHelper Instance => _instance.EnsureNotNull(nameof(_instance));

    public event Action<IServiceCollection>? OnRegisterServices;

    public WebApiRegistrationHelper()
    {
        _instance.EnsureNull();
        _instance = this;
    }

    #endregion

    #region Non-Public members

    private static IWebApiRegistrationHelper? _instance;

    #endregion
}
