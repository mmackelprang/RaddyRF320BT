using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Status request message
/// </summary>
public record StatusRequestMessage : BaseMessage
{
    /// <summary>
    /// Message type for status request
    /// </summary>
    public override MessageType MessageType => MessageType.STATUS_REQUEST;
    
    public StatusRequestMessage(int radioId = ProtocolConstants.DEFAULT_RADIO_ID) 
        : base(radioId)
    {
    }
}
