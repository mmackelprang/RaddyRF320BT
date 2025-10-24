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
    public override MessageType MessageType => MessageType.StatusRequest;
    
    public StatusRequestMessage(int radioId = ProtocolConstants.DefaultRadioId) 
        : base(radioId)
    {
    }
}
