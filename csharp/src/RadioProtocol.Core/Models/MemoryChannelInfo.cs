namespace RadioProtocol.Core.Models;

/// <summary>
/// Memory channel information
/// </summary>
public record MemoryChannelInfo
{
    public int ChannelNumber { get; init; }
    public string? ChannelName { get; init; }
    public string? Frequency { get; init; }
    public bool IsEmpty { get; init; }
    public bool IsLocked { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}