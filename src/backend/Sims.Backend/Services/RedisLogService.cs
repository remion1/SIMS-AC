using System.Text.Json;
using Sims.Backend.Models;
using StackExchange.Redis;

namespace Sims.Backend.Services;

public sealed class RedisLogService(IConnectionMultiplexer redis) : IRedisLogService
{
    // Redis-Key für alle SIMS-Logeinträge
    private const string RedisKey   = "sims:logs";
    private const int    MaxEntries = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        WriteIndented               = false,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private IDatabase Db => redis.GetDatabase();

    public async Task LogAsync(string level, string message, string? details = null)
    {
        var entry = new SimsLogEntry
        {
            LogLevel  = level.ToUpperInvariant(),
            Message   = message,
            Details   = details
        };

        var json = JsonSerializer.Serialize(entry, JsonOptions);

        var db = Db;
        await db.ListLeftPushAsync(RedisKey, json);
        await db.ListTrimAsync(RedisKey, 0, MaxEntries - 1);
    }

    public async Task<IEnumerable<SimsLogEntry>> GetRecentLogsAsync(int count = 50)
    {
        var values = await Db.ListRangeAsync(RedisKey, 0, count - 1);

        var entries = new List<SimsLogEntry>(values.Length);
        foreach (var value in values)
        {
            if (value.IsNullOrEmpty) continue;

            var entry = JsonSerializer.Deserialize<SimsLogEntry>(value.ToString(), JsonOptions);
            if (entry is not null)
                entries.Add(entry);
        }

        return entries;
    }
}
