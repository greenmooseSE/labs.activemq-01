namespace WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebApi.BackgroundServices;
using WebApi.Models;

[ApiController]
[Route("v1/[controller]")]
public class HealthController : ControllerBase
{
    #region Public members

    [HttpGet]
    public HealthVm Get()
    {
        _statService.HealthInvoked();
        _logger.LogTrace("GET /v1/health");
        return new HealthVm(_statService);
    }

    public HealthController(ILogger<HealthController> logger, IStatisticsService statService)
    {
        _logger = logger;
        _statService = statService;
    }

    #endregion

    #region Non-Public members

    private readonly ILogger<HealthController> _logger;
    private readonly IStatisticsService _statService;

    #endregion
}
