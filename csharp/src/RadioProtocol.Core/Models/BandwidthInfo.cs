namespace RadioProtocol.Core.Models;

/// <summary>
/// Bandwidth information from AB0D1C messages
/// </summary>
public record BandwidthInfo
{
    public int Value { get; init; }
    public string? Text { get; init; }
    public string? RawData { get; init; }
}
