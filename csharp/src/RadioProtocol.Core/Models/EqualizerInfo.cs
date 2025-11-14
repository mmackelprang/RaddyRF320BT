using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Models;

/// <summary>
/// Equalizer information from AB0C1C messages
/// </summary>
public record EqualizerInfo
{
    public int Value { get; init; }
    public EqualizerType EqualizerType { get; init; }
    public string? Text { get; init; }
    public string? RawData { get; init; }
}
