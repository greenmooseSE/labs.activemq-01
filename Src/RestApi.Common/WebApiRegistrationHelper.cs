namespace RestApi.Common;

using Microsoft.Extensions.DependencyInjection;
using RestApi.Common.EnsureExtension;

public class WebApiRegistrationHelper : IWebApiRegistrationHelper
{
    #region IWebApiRegistrationHelper members

    /// <inheritdoc />
    public bool DoRegisterHostedServices { get; set; } = true;

    /// <inheritdoc />
    public void RegisterServiceProvider(IServiceProvider serviceProvider)
    {
        OnRegisterServiceProvider?.Invoke(serviceProvider);
    }

    public void RegisterServices(IServiceCollection services)
    {
        OnRegisterServices?.Invoke(services);
    }

    #endregion

    #region Public members

    public static IWebApiRegistrationHelper Instance
    {
        get
        {
            // _instance = _instance ?? new WebApiRegistrationHelper();
            return _instance.EnsureNotNull(nameof(_instance));
        }
    }

    public event Action<IServiceProvider>? OnRegisterServiceProvider;

    public event Action<IServiceCollection>? OnRegisterServices;

    private WebApiRegistrationHelper()
    {
        _instance.EnsureNull();
        // _instance = this;
    }

    #endregion

    #region Non-Public members

    private static IWebApiRegistrationHelper? _instance=new WebApiRegistrationHelper();

    #endregion
}
