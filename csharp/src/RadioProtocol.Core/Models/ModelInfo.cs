namespace RadioProtocol.Core.Models;

/// <summary>
/// Model information from AB111C messages
/// </summary>
public record ModelInfo
{
    public int VersionNumber { get; init; }
    public string? VersionText { get; init; }
    public string? RawData { get; init; }
}
