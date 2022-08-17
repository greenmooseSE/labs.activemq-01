namespace WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/[controller]")]
public class HealthController : ControllerBase
{
    #region Public members

    public string Get()
    {
        _logger.LogTrace("GET /v1/health");
        return "Healthy";
    }

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    #endregion

    #region Non-Public members

    private readonly ILogger<HealthController> _logger;

    #endregion
}
