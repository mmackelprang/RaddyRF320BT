using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Models;

/// <summary>
/// Generic response packet
/// </summary>
public record ResponsePacket
{
    public ResponsePacketType PacketType { get; init; }
    public string CommandId { get; init; } = string.Empty;
    public byte[] RawData { get; init; } = Array.Empty<byte>();
    public string HexData { get; init; } = string.Empty;
    public object? ParsedData { get; init; }
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}