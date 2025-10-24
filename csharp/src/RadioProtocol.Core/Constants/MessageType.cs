namespace RadioProtocol.Core.Constants;

/// <summary>
/// Message types for protocol communication
/// </summary>
public enum MessageType : byte
{
    /// <summary>Button press command</summary>
    ButtonPress = 0x0C,
    
    /// <summary>Channel command</summary>
    ChannelCommand = 0x0D,
    
    /// <summary>Sync request message</summary>
    SyncRequest = 0x01,
    
    /// <summary>Sync response message</summary>
    SyncResponse = 0x02,
    
    /// <summary>Status request message</summary>
    StatusRequest = 0x03,
    
    /// <summary>Status response message</summary>
    StatusResponse = 0x04,
    
    /// <summary>General response message</summary>
    GeneralResponse = 0x05
}
