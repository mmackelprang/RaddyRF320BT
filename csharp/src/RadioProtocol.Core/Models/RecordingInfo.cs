namespace RadioProtocol.Core.Models;

/// <summary>
/// Recording status information
/// </summary>
public record RecordingInfo
{
    public bool IsRecording { get; init; }
    public int RecordIndex { get; init; }
    public TimeSpan? RecordingDuration { get; init; }
    public string? RecordingMode { get; init; }
    public string? RawData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}