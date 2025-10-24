using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Protocol;

/// <summary>
/// Utility functions for protocol message handling
/// </summary>
public static class ProtocolUtils
{
    /// <summary>
    /// Calculate checksum for protocol data
    /// </summary>
    /// <param name="data">Data bytes to checksum</param>
    /// <returns>Checksum byte (sum of all bytes & 0xFF)</returns>
    public static byte CalculateChecksum(byte[] data)
    {
        if (data == null || data.Length == 0)
            return 0x00;
        
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b & 0xFF;
        }
        return (byte)(sum & 0xFF);
    }

    /// <summary>
    /// Validate checksum of a message
    /// </summary>
    /// <param name="dataWithChecksum">Message data including checksum as last byte</param>
    /// <returns>True if checksum is valid</returns>
    public static bool ValidateChecksum(byte[] dataWithChecksum)
    {
        if (dataWithChecksum == null || dataWithChecksum.Length < 2)
            return false;

        // Split data and checksum
        var data = dataWithChecksum[..^1];
        var checksumByte = dataWithChecksum[^1];
        
        // Calculate expected checksum
        var expectedChecksum = CalculateChecksum(data);
        
        return checksumByte == expectedChecksum;
    }

    /// <summary>
    /// Parse hex string to byte array
    /// </summary>
    /// <param name="hexString">Hex string (e.g., "AB01FF")</param>
    /// <returns>Byte array</returns>
    public static byte[] ParseHexString(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return Array.Empty<byte>();

        hexString = hexString.Replace(" ", "").Replace("-", "");

        if (hexString.Length % 2 != 0)
            throw new FormatException("Hex string must have an even number of characters");

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// Convert byte array to hex string
    /// </summary>
    /// <param name="bytes">Byte array</param>
    /// <returns>Hex string (e.g., "AB01FF")</returns>
    public static string ToHexString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Check if data is a valid protocol message
    /// </summary>
    /// <param name="data">Message data</param>
    /// <returns>True if valid protocol message</returns>
    public static bool IsValidProtocolMessage(byte[] data)
    {
        if (data == null)
            return false;
        
        if (data.Length < 2)
            return false;

        // Check for protocol start byte (0xAB)
        return data[0] == 0xAB;
    }

    /// <summary>
    /// Get radio ID from message
    /// </summary>
    /// <param name="data">Message data</param>
    /// <returns>Radio ID byte</returns>
    public static byte GetRadioId(byte[] data)
    {
        if (data == null || data.Length < 3)
            throw new ArgumentException("Message too short to contain radio ID");

        if (data[0] != ProtocolConstants.ProtocolStartByte)
            throw new ArgumentException("Invalid protocol message");

        // For most messages, radio ID would be in a specific position
        // This is a simplified implementation
        return ProtocolConstants.DefaultRadioId;
    }

    /// <summary>
    /// Get message type from message data
    /// </summary>
    /// <param name="data">Message data</param>
    /// <returns>Message type byte</returns>
    public static byte GetMessageType(byte[] data)
    {
        if (data == null || data.Length < 3)
            throw new ArgumentException("Message too short");

        // Message type is typically at index 2 after start byte and length
        return data[2];
    }
}
