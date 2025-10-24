using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Base class for all protocol messages
/// </summary>
public abstract record BaseMessage
{
    /// <summary>
    /// Radio device ID
    /// </summary>
    public byte RadioId { get; init; }
    
    /// <summary>
    /// Message type
    /// </summary>
    public abstract MessageType MessageType { get; }
    
    protected BaseMessage(int radioId = ProtocolConstants.DefaultRadioId)
    {
        RadioId = (byte)radioId;
    }
}
