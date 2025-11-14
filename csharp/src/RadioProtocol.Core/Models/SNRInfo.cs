namespace RadioProtocol.Core.Models;

/// <summary>
/// SNR information from AB081C messages
/// </summary>
public record SNRInfo
{
    public int Value { get; init; }
    public string? Text { get; init; }
    public string? RawData { get; init; }
}
