namespace RadioProtocol.Core.Models;

/// <summary>
/// Battery and power information
/// </summary>
public record BatteryInfo
{
    public int BatteryPercentage { get; init; }
    public bool IsCharging { get; init; }
    public bool IsLowBattery { get; init; }
    public string? PowerSource { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}