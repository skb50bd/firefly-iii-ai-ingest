using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Core.Models.Responses;
using Brotal.FireflyBuddy.Core.Services;
using Brotal.FireflyBuddy.Core.Data;

namespace Brotal.FireflyBuddy.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class HealthController(
    IFireflyClient fireflyClient,
    ApplicationDbContext dbContext,
    ILogger<HealthController> logger
) : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Application health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Database.CanConnectAsync(cancellationToken);
        var context = await fireflyClient.GetContextAsync(cancellationToken);

        if (!result || context?.AssetAccounts?.Count is not > 0)
        {
            logger.LogError("Failed to connect to database or get context from Firefly III API");
            return BadRequest(new HealthCheckResponse("unhealthy", DateTime.UtcNow));
        }

        return Ok(new HealthCheckResponse("healthy", DateTime.UtcNow));
    }

    [HttpGet("ping")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public IActionResult GetPing()
    {
        return Ok(new HealthCheckResponse("pong", DateTime.UtcNow));
    }

    [HttpGet("firefly")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFireflyHealth(CancellationToken cancellationToken = default)
    {
        var context = await fireflyClient.GetContextAsync(cancellationToken);

        if (context?.AssetAccounts?.Count is not > 0)
        {
            logger.LogError("Failed to get context from Firefly III API");
            return BadRequest(new HealthCheckResponse("failed", DateTime.UtcNow));
        }

        return Ok(new HealthCheckResponse("healthy", DateTime.UtcNow));
    }

    [HttpGet("database")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = await dbContext.Database.CanConnectAsync(cancellationToken);

        if (!result)
        {
            logger.LogError("Failed to connect to database");
            return BadRequest(new HealthCheckResponse("failed", DateTime.UtcNow));
        }

        return Ok(new HealthCheckResponse("healthy", DateTime.UtcNow));
    }
}
