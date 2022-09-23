namespace RestApi.Common;

using Microsoft.Extensions.DependencyInjection;

public interface IWebApiRegistrationHelper
{
    #region Public members

    bool DoRegisterHostedServices { get; }

    void RegisterServiceProvider(IServiceProvider serviceProvider);

    void RegisterServices(IServiceCollection services);

    #endregion
}
