using System;

namespace RadioProtocol.Core.Protocol;

/// <summary>
/// Radio protocol command groups
/// </summary>
public enum CommandGroup : byte 
{ 
    Button = 0x0C, 
    Ack = 0x12, 
    Status = 0x1C 
}

/// <summary>
/// Base constants for radio protocol
/// </summary>
public static class CommandBase
{
    public const byte Header = 0xAB;
    public const byte Proto = 0x02;
    
    public static byte BaseFor(CommandGroup g) => g switch
    {
        CommandGroup.Button => 0xB9,
        CommandGroup.Ack => 0xBF,
        CommandGroup.Status => 0x00, // Status messages don't use standard checksum
        _ => throw new ArgumentOutOfRangeException(nameof(g))
    };
}

/// <summary>
/// Represents a radio protocol frame
/// </summary>
public record RadioFrame(byte Header, byte Proto, CommandGroup Group, byte CommandId, byte Check)
{
    /// <summary>
    /// Builds a radio frame with automatic checksum calculation
    /// </summary>
    public static RadioFrame Build(CommandGroup group, byte cmdId)
    {
        byte check = (byte)(CommandBase.BaseFor(group) + cmdId);
        return new RadioFrame(CommandBase.Header, CommandBase.Proto, group, cmdId, check);
    }

    /// <summary>
    /// Converts frame to byte array
    /// </summary>
    public byte[] ToBytes() => new[] { Header, Proto, (byte)Group, CommandId, Check };

    /// <summary>
    /// Attempts to parse a frame from raw data
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out RadioFrame? frame)
    {
        frame = default;
        
        // Handshake (length 4): AB 01 FF AB
        if (data.Length == 4 && data[0] == 0xAB && data[1] == 0x01 && data[3] == 0xAB)
        {
            frame = new RadioFrame(data[0], data[1], 0, data[2], data[3]);
            return true;
        }
        
        // Standard 5-byte frame (Button/Ack groups)
        if (data.Length == 5 && data[0] == CommandBase.Header && data[1] == CommandBase.Proto)
        {
            var group = (CommandGroup)data[2];
            if (group == CommandGroup.Button || group == CommandGroup.Ack)
            {
                byte baseVal = CommandBase.BaseFor(group);
                byte cmd = data[3];
                byte check = data[4];
                if (check != (byte)(baseVal + cmd)) return false;
                frame = new RadioFrame(data[0], data[1], group, cmd, check);
                return true;
            }
        }
        
        // Status messages: Variable length (AB-0X-1C-...) where 0X is length indicator
        if (data.Length >= 5 && data[0] == CommandBase.Header && data[2] == (byte)CommandGroup.Status)
        {
            byte lengthByte = data[1];
            // Create a status frame (proto field repurposed as length for status messages)
            frame = new RadioFrame(data[0], lengthByte, CommandGroup.Status, data[3], 
                data.Length > 4 ? data[4] : (byte)0);
            return true;
        }
        
        return false;
    }
}
