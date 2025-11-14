namespace RadioProtocol.Core.Models;

/// <summary>
/// Volume information from AB071C messages
/// </summary>
public record VolInfo
{
    public int Value { get; init; }
    public string? Text { get; init; }
    public string? RawData { get; init; }
}
