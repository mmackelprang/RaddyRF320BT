using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Models;

/// <summary>
/// Connection status information
/// </summary>
public record ConnectionInfo
{
    public ConnectionState State { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceAddress { get; init; }
    public string? ServiceUuid { get; init; }
    public TimeSpan? ConnectionDuration { get; init; }
    public int SignalStrength { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}