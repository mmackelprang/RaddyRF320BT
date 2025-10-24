namespace RadioProtocol.Core.Constants;

/// <summary>
/// Message types for protocol communication
/// </summary>
public enum MessageType : byte
{
    /// <summary>Button press command</summary>
    BUTTON_PRESS = 0x0C,
    
    /// <summary>Channel command</summary>
    CHANNEL_COMMAND = 0x0D,
    
    /// <summary>Sync request message</summary>
    SYNC_REQUEST = 0x01,
    
    /// <summary>Sync response message</summary>
    SYNC_RESPONSE = 0x02,
    
    /// <summary>Status request message</summary>
    STATUS_REQUEST = 0x03,
    
    /// <summary>Status response message</summary>
    STATUS_RESPONSE = 0x04,
    
    /// <summary>General response message</summary>
    GENERAL_RESPONSE = 0x05
}
