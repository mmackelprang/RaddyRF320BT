namespace RadioProtocol.Core.Models;

/// <summary>
/// Command execution result
/// </summary>
public record CommandResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public byte[]? SentData { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public ResponsePacket? Response { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}