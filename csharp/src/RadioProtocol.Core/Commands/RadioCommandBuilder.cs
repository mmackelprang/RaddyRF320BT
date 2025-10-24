using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Logging;

namespace RadioProtocol.Core.Commands;

/// <summary>
/// Radio command builder - simplifies command creation compared to Java implementation
/// </summary>
public class RadioCommandBuilder
{
    private readonly IRadioLogger _logger;

    public RadioCommandBuilder(IRadioLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Build a button press command
    /// </summary>
    /// <param name="buttonType">Type of button to press</param>
    /// <returns>Command packet bytes</returns>
    public byte[] BuildButtonCommand(ButtonType buttonType)
    {
        var command = BuildCommand(ProtocolConstants.CommandTypeButton, (byte)buttonType);
        _logger.LogMessageSent("ButtonPress", new { ButtonType = buttonType, CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build handshake command
    /// </summary>
    /// <returns>Handshake command packet</returns>
    public byte[] BuildHandshakeCommand()
    {
        var command = new byte[]
        {
            ProtocolConstants.ProtocolStartByte,
            ProtocolConstants.MessageLengthHandshake,
            ProtocolConstants.DataHandshake,
            ProtocolConstants.ProtocolStartByte
        };
        
        _logger.LogMessageSent("Handshake", new { CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build sync request command
    /// </summary>
    /// <returns>Sync request command packet</returns>
    public byte[] BuildSyncRequestCommand()
    {
        var command = BuildCommand((byte)MessageType.SyncRequest, 0x00);
        _logger.LogMessageSent("SyncRequest", new { CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build status request command
    /// </summary>
    /// <returns>Status request command packet</returns>
    public byte[] BuildStatusRequestCommand()
    {
        var command = BuildCommand((byte)MessageType.StatusRequest, 0x00);
        _logger.LogMessageSent("StatusRequest", new { CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build acknowledgment success response
    /// </summary>
    /// <returns>ACK success packet</returns>
    public byte[] BuildAckSuccessCommand()
    {
        var command = BuildCommand(ProtocolConstants.CommandTypeAck, ProtocolConstants.DataSuccess);
        _logger.LogMessageSent("AckSuccess", new { CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build acknowledgment failure response
    /// </summary>
    /// <returns>ACK failure packet</returns>
    public byte[] BuildAckFailureCommand()
    {
        var command = BuildCommand(ProtocolConstants.CommandTypeAck, ProtocolConstants.DataFailure);
        _logger.LogMessageSent("AckFailure", new { CommandHex = Convert.ToHexString(command) });
        return command;
    }

    /// <summary>
    /// Build a generic command with automatic checksum calculation
    /// </summary>
    /// <param name="commandType">Type of command</param>
    /// <param name="commandData">Command-specific data byte</param>
    /// <returns>Complete command packet with checksum</returns>
    public byte[] BuildCommand(byte commandType, byte commandData)
    {
        var cmd = new byte[ProtocolConstants.CommandPacketSize];
        cmd[0] = ProtocolConstants.ProtocolStartByte;
        cmd[1] = ProtocolConstants.MessageLengthStandard;
        cmd[2] = commandType;
        cmd[3] = commandData;
        
        // Calculate checksum (sum of first 4 bytes)
        int checksum = (cmd[0] & 0xFF) + (cmd[1] & 0xFF) + (cmd[2] & 0xFF) + (cmd[3] & 0xFF);
        cmd[4] = (byte)(checksum & 0xFF);
        
        return cmd;
    }

    /// <summary>
    /// Calculate checksum for command data
    /// </summary>
    /// <param name="data">Command data bytes</param>
    /// <returns>Checksum byte</returns>
    public static byte CalculateChecksum(byte[] data)
    {
        int sum = 0;
        foreach (byte b in data)
        {
            sum += (b & 0xFF);
        }
        return (byte)(sum & 0xFF);
    }

    /// <summary>
    /// Verify command checksum
    /// </summary>
    /// <param name="command">Command packet to verify</param>
    /// <returns>True if checksum is valid</returns>
    public static bool VerifyChecksum(byte[] command)
    {
        if (command == null || command.Length < ProtocolConstants.CommandPacketSize)
        {
            return false;
        }
        
        int expectedChecksum = 0;
        for (int i = 0; i < command.Length - 1; i++)
        {
            expectedChecksum += (command[i] & 0xFF);
        }
        
        return (byte)(expectedChecksum & 0xFF) == command[^1];
    }

    /// <summary>
    /// Convert byte array to hex string for debugging
    /// </summary>
    /// <param name="bytes">Byte array to convert</param>
    /// <returns>Hex string representation</returns>
    public static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes);
    }
}

/// <summary>
/// Predefined common radio commands
/// </summary>
public static class CommonCommands
{
    /// <summary>Get command for number button press</summary>
    public static byte[] NumberButton(int number, IRadioLogger logger)
    {
        var builder = new RadioCommandBuilder(logger);
        var buttonType = number switch
        {
            0 => ButtonType.Number0,
            1 => ButtonType.Number1,
            2 => ButtonType.Number2,
            3 => ButtonType.Number3,
            4 => ButtonType.Number4,
            5 => ButtonType.Number5,
            6 => ButtonType.Number6,
            7 => ButtonType.Number7,
            8 => ButtonType.Number8,
            9 => ButtonType.Number9,
            _ => throw new ArgumentException($"Invalid number: {number}")
        };
        return builder.BuildButtonCommand(buttonType);
    }

    /// <summary>Get command for number button long press (memory channel)</summary>
    public static byte[] NumberButtonLong(int number, IRadioLogger logger)
    {
        var builder = new RadioCommandBuilder(logger);
        var buttonType = number switch
        {
            0 => ButtonType.Number0Long,
            1 => ButtonType.Number1Long,
            2 => ButtonType.Number2Long,
            3 => ButtonType.Number3Long,
            4 => ButtonType.Number4Long,
            5 => ButtonType.Number5Long,
            6 => ButtonType.Number6Long,
            7 => ButtonType.Number7Long,
            8 => ButtonType.Number8Long,
            9 => ButtonType.Number9Long,
            _ => throw new ArgumentException($"Invalid number: {number}")
        };
        return builder.BuildButtonCommand(buttonType);
    }

    /// <summary>Get command for function key press</summary>
    public static byte[] FunctionKey(int keyNumber, IRadioLogger logger)
    {
        var builder = new RadioCommandBuilder(logger);
        var buttonType = keyNumber switch
        {
            1 => ButtonType.FunctionKey1,
            2 => ButtonType.FunctionKey2,
            3 => ButtonType.FunctionKey3,
            4 => ButtonType.FunctionKey4,
            5 => ButtonType.FunctionKey5,
            _ => throw new ArgumentException($"Invalid function key: {keyNumber}")
        };
        return builder.BuildButtonCommand(buttonType);
    }

    /// <summary>Volume control commands</summary>
    public static class Volume
    {
        public static byte[] Up(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.VolumeUp);
        
        public static byte[] Down(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.VolumeDown);
    }

    /// <summary>Navigation commands</summary>
    public static class Navigation
    {
        public static byte[] Up(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.UpShort);
        
        public static byte[] Down(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.DownShort);
        
        public static byte[] UpLong(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.UpLong);
        
        public static byte[] DownLong(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.DownLong);
        
        public static byte[] Back(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.Back);
    }

    /// <summary>Power commands</summary>
    public static class Power
    {
        public static byte[] Toggle(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.Power);
        
        public static byte[] Off(IRadioLogger logger) => 
            new RadioCommandBuilder(logger).BuildButtonCommand(ButtonType.PowerLong);
    }
}