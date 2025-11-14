namespace RadioProtocol.Core.Models;

/// <summary>
/// Radio version information from AB091C messages
/// </summary>
public record RadioVersionInfo
{
    public int VersionNumber { get; init; }
    public string? VersionText { get; init; }
    public string? RawData { get; init; }
}
