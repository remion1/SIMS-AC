using Sims.Backend.Models;

namespace Sims.Backend.Services;

public interface IRedisLogService
{
    Task LogAsync(string level, string message, string? details = null);
    Task<IEnumerable<SimsLogEntry>> GetRecentLogsAsync(int count = 50);
}
