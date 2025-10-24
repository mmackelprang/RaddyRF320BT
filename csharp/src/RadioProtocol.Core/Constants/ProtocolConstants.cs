namespace RadioProtocol.Core.Constants;

/// <summary>
/// Radio protocol constants for command structure and data values
/// </summary>
public static class ProtocolConstants
{
    // ==================== PROTOCOL STRUCTURE ====================
    
    /// <summary>Protocol start byte (0xAB)</summary>
    public const byte ProtocolStartByte = 0xAB;
    
    /// <summary>Message version</summary>
    public const byte MessageVersion = 0x20;
    
    /// <summary>Default radio ID</summary>
    public const byte DefaultRadioId = 0x01;
    
    /// <summary>Maximum message length</summary>
    public const int MaxMessageLength = 255;
    
    /// <summary>Minimum message length</summary>
    public const int MinMessageLength = 4;
    
    /// <summary>Standard message length for button commands</summary>
    public const byte MessageLengthStandard = 0x02;
    
    /// <summary>Message length for handshake commands</summary>
    public const byte MessageLengthHandshake = 0x01;
    
    /// <summary>Standard command packet size</summary>
    public const int CommandPacketSize = 5;
    
    /// <summary>Minimum packet length for valid commands</summary>
    public const int MinPacketLength = 6;
    
    /// <summary>Command identifier length (first 6 hex characters)</summary>
    public const int CommandIdLength = 6;
    
    
    // ==================== COMMAND TYPES ====================
    
    /// <summary>Standard command type for button presses</summary>
    public const byte CommandTypeButton = 0x0C;
    
    /// <summary>Command type for handshake/acknowledgment</summary>
    public const byte CommandTypeHandshake = 0x01;
    
    /// <summary>Command type for acknowledgment response</summary>
    public const byte CommandTypeAck = 0x12;
    
    
    // ==================== COMMON DATA BYTES ====================
    
    /// <summary>Handshake data byte</summary>
    public const byte DataHandshake = 0xFF;
    
    /// <summary>Success response data</summary>
    public const byte DataSuccess = 0x01;
    
    /// <summary>Failure response data</summary>
    public const byte DataFailure = 0x00;
    
    
    // ==================== PACKET LENGTH CONSTANTS ====================
    
    /// <summary>Minimum length for frequency status packets</summary>
    public const int MinFreqStatusLength = 12;
    
    /// <summary>Minimum length for band info packets</summary>
    public const int MinBandInfoLength = 32;
    
    /// <summary>Minimum length for standard status packets</summary>
    public const int MinStatusLength = 16;
}