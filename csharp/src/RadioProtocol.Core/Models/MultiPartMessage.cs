namespace RadioProtocol.Core.Models;

/// <summary>
/// Multi-part message assembly helper
/// </summary>
public record MultiPartMessage
{
    public string MessageType { get; init; } = string.Empty;
    public int TotalParts { get; init; }
    public int CurrentPart { get; init; }
    public string PartialData { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
    public DateTime StartTime { get; init; } = DateTime.Now;
    public DateTime LastUpdate { get; init; } = DateTime.Now;
}