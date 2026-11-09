using Microsoft.AspNetCore.Mvc;
using NewsApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace NewsApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ProviderStatisticsController : ControllerBase
{
    private readonly IProviderStatisticsService _statistics;

    public ProviderStatisticsController(IProviderStatisticsService statistics)
    {
        _statistics = statistics;
    }

    [HttpGet]
    public IActionResult GetStatistics()
    {
        return Ok(_statistics.GetAll());
    }
}