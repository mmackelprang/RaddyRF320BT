namespace RadioProtocol.Core.Models;

/// <summary>
/// Demodulation information from AB101C messages
/// </summary>
public record DemodulationInfo
{
    public int Value { get; init; }
    public string? Text { get; init; }
    public string? RawData { get; init; }
}
