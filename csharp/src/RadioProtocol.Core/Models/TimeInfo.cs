namespace RadioProtocol.Core.Models;

/// <summary>
/// Time and clock information
/// </summary>
public record TimeInfo
{
    public DateTime? RadioTime { get; init; }
    public TimeZoneInfo? TimeZone { get; init; }
    public bool IsTimeValid { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}