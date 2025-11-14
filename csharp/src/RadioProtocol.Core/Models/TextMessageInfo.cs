namespace RadioProtocol.Core.Models;

/// <summary>
/// Text message information from AB11 multi-part messages
/// </summary>
public record TextMessageInfo
{
    public string Message { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string? RawData { get; init; }
}
