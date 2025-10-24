namespace RadioProtocol.Core.Models;

/// <summary>
/// Frequency and band information
/// </summary>
public record FrequencyInfo
{
    public string? FrequencyHz { get; init; }
    public string? BandCode { get; init; }
    public string? SubBand1 { get; init; }
    public string? SubBand2 { get; init; }
    public string? SubBand3 { get; init; }
    public string? SubBand4 { get; init; }
    public string? ChannelName { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}