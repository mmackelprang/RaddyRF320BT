using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Models;

/// <summary>
/// Parsed radio status data
/// </summary>
public record RadioStatus
{
    public string? Frequency { get; init; }        // Current frequency in Hz (as string)
    public string? Band { get; init; }             // Band identifier (e.g., "06" for VHF)
    public string? SubBand { get; init; }          // Sub-band information
    public string? Demodulation { get; init; }     // Demodulation mode
    public string? Bandwidth { get; init; }        // Bandwidth setting
    public int SquelchLevel { get; init; }         // Squelch level (0-15)
    public int VolumeLevel { get; init; }          // Volume level (0-15)
    public bool IsStereo { get; init; }            // Stereo mode enabled
    public bool IsPowerOn { get; init; }           // Power state
    public string? RawData { get; init; }          // Raw hex data for debugging
    public DateTime Timestamp { get; init; } = DateTime.Now;
}