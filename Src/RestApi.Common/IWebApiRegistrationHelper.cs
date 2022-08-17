using System;
using System.Linq;

namespace RestApi.Common;

using Microsoft.Extensions.DependencyInjection;

public interface IWebApiRegistrationHelper
{
    #region Public members

    void RegisterServices(IServiceCollection services);

    #endregion
}
