using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Response message from radio
/// </summary>
public record ResponseMessage
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Response data bytes
    /// </summary>
    public byte[] Data { get; init; }
    
    /// <summary>
    /// Message type of the response
    /// </summary>
    public MessageType MessageType { get; init; }
    
    public ResponseMessage(bool success, byte[] data, MessageType messageType)
    {
        Success = success;
        Data = data ?? Array.Empty<byte>();
        MessageType = messageType;
    }
}
