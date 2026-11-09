using Microsoft.AspNetCore.Mvc;
using NewsApi.Application.Interfaces;
using NewsApi.Application.Models;
using Microsoft.AspNetCore.Authorization;

namespace NewsApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed([FromQuery] FeedRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var feedItems = await _feedService.GetFeedAsync(
                request,
                cancellationToken);

            return Ok(feedItems);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}