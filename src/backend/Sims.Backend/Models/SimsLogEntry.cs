namespace Sims.Backend.Models;

public sealed class SimsLogEntry
{
    public string   Id        { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string   LogLevel  { get; set; } = "INFO";
    public string   Message   { get; set; } = string.Empty;
    public string?  Details   { get; set; }
}
