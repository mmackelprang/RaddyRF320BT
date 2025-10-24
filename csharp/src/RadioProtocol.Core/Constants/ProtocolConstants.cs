namespace RadioProtocol.Core.Constants;

/// <summary>
/// Radio protocol constants for command structure and data values
/// </summary>
public static class ProtocolConstants
{
    // ==================== PROTOCOL STRUCTURE ====================
    
    /// <summary>Protocol start byte (0xAB)</summary>
    public const byte PROTOCOL_START_BYTE = 0xAB;
    
    /// <summary>Standard message length for button commands</summary>
    public const byte MESSAGE_LENGTH_STANDARD = 0x02;
    
    /// <summary>Message length for handshake commands</summary>
    public const byte MESSAGE_LENGTH_HANDSHAKE = 0x01;
    
    /// <summary>Standard command packet size</summary>
    public const int COMMAND_PACKET_SIZE = 5;
    
    /// <summary>Minimum packet length for valid commands</summary>
    public const int MIN_PACKET_LENGTH = 6;
    
    /// <summary>Command identifier length (first 6 hex characters)</summary>
    public const int COMMAND_ID_LENGTH = 6;
    
    
    // ==================== COMMAND TYPES ====================
    
    /// <summary>Standard command type for button presses</summary>
    public const byte COMMAND_TYPE_BUTTON = 0x0C;
    
    /// <summary>Command type for handshake/acknowledgment</summary>
    public const byte COMMAND_TYPE_HANDSHAKE = 0x01;
    
    /// <summary>Command type for acknowledgment response</summary>
    public const byte COMMAND_TYPE_ACK = 0x12;
    
    
    // ==================== COMMON DATA BYTES ====================
    
    /// <summary>Handshake data byte</summary>
    public const byte DATA_HANDSHAKE = 0xFF;
    
    /// <summary>Success response data</summary>
    public const byte DATA_SUCCESS = 0x01;
    
    /// <summary>Failure response data</summary>
    public const byte DATA_FAILURE = 0x00;
    
    
    // ==================== PACKET LENGTH CONSTANTS ====================
    
    /// <summary>Minimum length for frequency status packets</summary>
    public const int MIN_FREQ_STATUS_LENGTH = 12;
    
    /// <summary>Minimum length for band info packets</summary>
    public const int MIN_BAND_INFO_LENGTH = 32;
    
    /// <summary>Minimum length for standard status packets</summary>
    public const int MIN_STATUS_LENGTH = 16;
}