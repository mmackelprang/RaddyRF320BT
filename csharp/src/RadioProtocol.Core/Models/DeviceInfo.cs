namespace RadioProtocol.Core.Models;

/// <summary>
/// Device information from multi-part ASCII messages
/// </summary>
public record DeviceInfo
{
    public string? RadioVersion { get; init; }
    public string? ModelName { get; init; }
    public string? ContactInfo { get; init; }
    public string? SerialNumber { get; init; }
    public string? FirmwareVersion { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}