using Microsoft.AspNetCore.Mvc;
using Sims.Backend.Services;

namespace Sims.Backend.Controllers;

/// <summary>Exposes recent structured log entries stored in Redis.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class LogsController(IRedisLogService logService) : ControllerBase
{
    // ── GET /api/logs?count=50 ────────────────────────────────────────────────

    /// <summary>
    /// Returns the most recent log entries from Redis.
    /// </summary>
    /// <param name="count">Maximum number of entries to return (1–500, default 50).</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLogs([FromQuery] int count = 50)
    {
        if (count is < 1 or > 500)
            return BadRequest(new { Message = "count must be between 1 and 500." });

        var logs = await logService.GetRecentLogsAsync(count);
        return Ok(logs);
    }
}
