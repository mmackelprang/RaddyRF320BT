namespace RadioProtocol.Core.Models;

/// <summary>
/// Bandwidth and modulation information
/// </summary>
public record ModulationInfo
{
    public string? ModulationType { get; init; }  // AM, FM, etc.
    public string? BandwidthSetting { get; init; }
    public bool IsNarrowband { get; init; }
    public bool IsWideband { get; init; }
    public string? FilterType { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}