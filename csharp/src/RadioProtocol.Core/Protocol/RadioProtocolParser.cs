using System.Text;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Logging;

namespace RadioProtocol.Core.Protocol;

/// <summary>
/// Radio protocol data parser - converts raw bytes to typed objects
/// </summary>
public class RadioProtocolParser
{
    private readonly IRadioLogger _logger;
    private readonly Dictionary<string, MultiPartMessage> _multiPartMessages = new();

    // Command type mapping
    private static readonly Dictionary<string, ResponsePacketType> CommandTypeMap = new()
    {
        ["ab0417"] = ResponsePacketType.FrequencyStatus,
        ["ab031e"] = ResponsePacketType.Time,
        ["ab0901"] = ResponsePacketType.BandInfo,
        ["ab0303"] = ResponsePacketType.Volume,
        ["ab031f"] = ResponsePacketType.Signal,
        ["ab090f"] = ResponsePacketType.FrequencyInput,
        ["ab11"] = ResponsePacketType.DeviceInfo,
        ["ab10"] = ResponsePacketType.DeviceInfoCont,
        ["ab0e"] = ResponsePacketType.SubBandInfo,
        ["ab08"] = ResponsePacketType.LockStatus,
        ["ab0b"] = ResponsePacketType.RecordingStatus,
        ["ab02"] = ResponsePacketType.StatusShort,
        ["ab05"] = ResponsePacketType.FreqData1,
        ["ab06"] = ResponsePacketType.FreqData2,
        ["ab07"] = ResponsePacketType.Battery,
        ["ab09"] = ResponsePacketType.DetailedFreq,
        ["ab0d"] = ResponsePacketType.Bandwidth
    };

    public RadioProtocolParser(IRadioLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse incoming data from radio
    /// </summary>
    /// <param name="data">Raw byte data received</param>
    /// <returns>Parsed response packet</returns>
    public ResponsePacket ParseReceivedData(byte[] data)
    {
        _logger.LogRawDataReceived(data);

        if (data == null || data.Length < ProtocolConstants.MinPacketLength)
        {
            _logger.LogWarning($"Invalid data packet received - length: {data?.Length ?? 0}");
            return new ResponsePacket 
            { 
                IsValid = false, 
                ErrorMessage = "Invalid packet length",
                RawData = data ?? Array.Empty<byte>()
            };
        }

        var hexString = Convert.ToHexString(data).ToLowerInvariant();
        _logger.LogDebug($"Parsing data: {hexString}");

        // Extract command identifier (first 6 characters)
        var commandId = hexString.Length >= ProtocolConstants.CommandIdLength 
            ? hexString[..ProtocolConstants.CommandIdLength] 
            : hexString;

        var packetType = CommandTypeMap.GetValueOrDefault(commandId, ResponsePacketType.Unknown);
        
        var responsePacket = new ResponsePacket
        {
            PacketType = packetType,
            CommandId = commandId,
            RawData = data,
            HexData = hexString,
            IsValid = true
        };

        try
        {
            responsePacket = responsePacket with 
            { 
                ParsedData = packetType switch
                {
                    ResponsePacketType.FrequencyStatus => ParseFrequencyStatus(hexString),
                    ResponsePacketType.Time => ParseTimeUpdate(hexString),
                    ResponsePacketType.BandInfo => ParseBandInfo(hexString),
                    ResponsePacketType.Volume => ParseVolumeLevel(hexString),
                    ResponsePacketType.Signal => ParseSignalStrength(hexString),
                    ResponsePacketType.FrequencyInput => ParseFrequencyInput(hexString),
                    ResponsePacketType.DeviceInfo or ResponsePacketType.DeviceInfoCont => ParseDeviceInfo(hexString),
                    ResponsePacketType.SubBandInfo => ParseSubBandInfo(hexString),
                    ResponsePacketType.LockStatus => ParseLockStatus(hexString),
                    ResponsePacketType.RecordingStatus => ParseRecordingStatus(hexString),
                    ResponsePacketType.StatusShort => ParseStatusShort(hexString),
                    ResponsePacketType.FreqData1 => ParseFreqData1(hexString),
                    ResponsePacketType.FreqData2 => ParseFreqData2(hexString),
                    ResponsePacketType.Battery => ParseBattery(hexString),
                    ResponsePacketType.DetailedFreq => ParseDetailedFreq(hexString),
                    ResponsePacketType.Bandwidth => ParseBandwidth(hexString),
                    _ => ParseUnknownPacket(hexString)
                }
            };

            if (responsePacket.ParsedData != null)
            {
                _logger.LogMessageReceived(packetType.ToString(), responsePacket.ParsedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing packet type {packetType}");
            responsePacket = responsePacket with 
            { 
                IsValid = false, 
                ErrorMessage = ex.Message 
            };
        }

        return responsePacket;
    }

    private RadioStatus ParseFrequencyStatus(string hexData)
    {
        if (hexData.Length < ProtocolConstants.MinFreqStatusLength)
        {
            _logger.LogWarning("Frequency status packet too short");
            return new RadioStatus { RawData = hexData };
        }

        try
        {
            // Extract status bytes (positions 6-11)
            var byte1 = hexData.Length > 7 ? hexData[6..8] : "";
            var byte2 = hexData.Length > 9 ? hexData[8..10] : "";
            var byte3 = hexData.Length > 11 ? hexData[10..12] : "";

            _logger.LogDebug($"Status bytes: {byte1} {byte2} {byte3}");

            return new RadioStatus
            {
                RawData = hexData,
                // Additional parsing logic would go here based on actual packet structure
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing frequency status");
            return new RadioStatus { RawData = hexData };
        }
    }

    private FrequencyInfo ParseBandInfo(string hexData)
    {
        if (hexData.Length < ProtocolConstants.MinBandInfoLength)
        {
            _logger.LogWarning("Band info packet too short");
            return new FrequencyInfo { RawData = hexData };
        }

        try
        {
            // Extract band identifier (positions 6-7)
            var bandCode = hexData.Length > 7 ? hexData[6..8] : "";

            // Extract sub-bands (positions 8-15)
            var subBand1 = hexData.Length > 9 ? hexData[8..10] : "";
            var subBand2 = hexData.Length > 11 ? hexData[10..12] : "";
            var subBand3 = hexData.Length > 13 ? hexData[12..14] : "";
            var subBand4 = hexData.Length > 15 ? hexData[14..16] : "";

            return new FrequencyInfo
            {
                BandCode = bandCode,
                SubBand1 = subBand1,
                SubBand2 = subBand2,
                SubBand3 = subBand3,
                SubBand4 = subBand4,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing band info");
            return new FrequencyInfo { RawData = hexData };
        }
    }

    private DeviceInfo ParseDeviceInfo(string hexData)
    {
        try
        {
            // Device info packets contain ASCII text
            // Extract ASCII portion and convert to string
            if (hexData.Length > 16)
            {
                var asciiHex = hexData[16..]; // Skip header bytes
                var asciiText = HexToAscii(asciiHex);
                
                _logger.LogDebug($"Device info ASCII: {asciiText}");
                
                return new DeviceInfo
                {
                    RawData = hexData,
                    // Parse specific fields based on message content
                    RadioVersion = asciiText.Contains("Radio version") ? asciiText : null,
                    ModelName = asciiText.Contains("Model") ? asciiText : null,
                    ContactInfo = asciiText.Contains("Contact") ? asciiText : null
                };
            }

            return new DeviceInfo { RawData = hexData };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing device info");
            return new DeviceInfo { RawData = hexData };
        }
    }

    private AudioInfo ParseVolumeLevel(string hexData)
    {
        // Implementation for volume parsing
        return new AudioInfo { RawData = hexData };
    }

    private AudioInfo ParseSignalStrength(string hexData)
    {
        // Implementation for signal strength parsing
        return new AudioInfo { RawData = hexData };
    }

    private object ParseFrequencyInput(string hexData)
    {
        // Implementation for frequency input parsing
        return new { RawData = hexData };
    }

    private object ParseSubBandInfo(string hexData)
    {
        // Implementation for sub-band info parsing
        return new { RawData = hexData };
    }

    private object ParseLockStatus(string hexData)
    {
        // Implementation for lock status parsing
        return new { RawData = hexData };
    }

    private RecordingInfo ParseRecordingStatus(string hexData)
    {
        // Implementation for recording status parsing
        return new RecordingInfo { RawData = hexData };
    }

    private object ParseStatusShort(string hexData)
    {
        // Implementation for short status parsing
        return new { RawData = hexData };
    }

    private object ParseFreqData1(string hexData)
    {
        // Implementation for frequency data 1 parsing
        return new { RawData = hexData };
    }

    private object ParseFreqData2(string hexData)
    {
        // Implementation for frequency data 2 parsing
        return new { RawData = hexData };
    }

    private BatteryInfo ParseBattery(string hexData)
    {
        // Implementation for battery parsing
        return new BatteryInfo { RawData = hexData };
    }

    private object ParseDetailedFreq(string hexData)
    {
        // Implementation for detailed frequency parsing
        return new { RawData = hexData };
    }

    private ModulationInfo ParseBandwidth(string hexData)
    {
        // Implementation for bandwidth parsing
        return new ModulationInfo { RawData = hexData };
    }

    private TimeInfo ParseTimeUpdate(string hexData)
    {
        // Implementation for time update parsing
        return new TimeInfo { RawData = hexData };
    }

    private object ParseUnknownPacket(string hexData)
    {
        _logger.LogWarning($"Unknown packet type: {hexData[..Math.Min(6, hexData.Length)]}");
        return new { RawData = hexData, PacketType = "Unknown" };
    }

    /// <summary>
    /// Convert hex string to ASCII text
    /// </summary>
    /// <param name="hex">Hex string</param>
    /// <returns>ASCII text</returns>
    private static string HexToAscii(string hex)
    {
        if (hex.Length % 2 != 0)
            return string.Empty;

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return Encoding.ASCII.GetString(bytes);
    }

    /// <summary>
    /// Convert byte array to hex string for debugging
    /// </summary>
    /// <param name="bytes">Byte array to convert</param>
    /// <returns>Hex string representation</returns>
    public static string BytesToHexString(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}