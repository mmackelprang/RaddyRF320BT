namespace RadioProtocol.Core.Models;

/// <summary>
/// Audio and volume information
/// </summary>
public record AudioInfo
{
    public int VolumeLevel { get; init; }
    public int SignalStrength { get; init; }
    public bool IsStereo { get; init; }
    public bool IsMuted { get; init; }
    public string? AudioMode { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}