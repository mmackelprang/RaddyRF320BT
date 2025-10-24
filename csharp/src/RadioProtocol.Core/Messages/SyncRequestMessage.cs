using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Sync request message
/// </summary>
public record SyncRequestMessage : BaseMessage
{
    /// <summary>
    /// Message type for sync request
    /// </summary>
    public override MessageType MessageType => MessageType.SyncRequest;
    
    public SyncRequestMessage(int radioId = ProtocolConstants.DefaultRadioId) 
        : base(radioId)
    {
    }
}
