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
        ["ab0d"] = ResponsePacketType.Bandwidth,
        ["ab1109"] = ResponsePacketType.TextMessage,  // AB11 text messages
        ["ab101c"] = ResponsePacketType.Demodulation,
        ["ab0d1c"] = ResponsePacketType.Bandwidth,
        ["ab081c"] = ResponsePacketType.SNR,
        ["ab071c"] = ResponsePacketType.Vol,
        ["ab111c"] = ResponsePacketType.Model,
        ["ab091c"] = ResponsePacketType.RadioVersion,
        ["ab0c1c"] = ResponsePacketType.EqualizerSettings,
        ["ab061c"] = ResponsePacketType.Heartbeat,
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

        // Extract command identifier and try to find matching packet type
        var commandId = hexString.Length >= ProtocolConstants.CommandIdLength 
            ? hexString[..ProtocolConstants.CommandIdLength] 
            : hexString;

        // Try to match command type - look for exact matches in the map at different lengths
        var packetType = ResponsePacketType.Unknown;
        
        // Try progressively shorter prefixes, preferring longer exact matches
        for (int len = Math.Min(hexString.Length, 6); len >= 4 && packetType == ResponsePacketType.Unknown; len--)
        {
            var prefix = hexString[..len];
            if (CommandTypeMap.ContainsKey(prefix))
            {
                packetType = CommandTypeMap[prefix];
                break;
            }
        }
        
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
                    ResponsePacketType.TextMessage => ParseTextMessage(hexString),
                    ResponsePacketType.Demodulation => ParseDemodulation(hexString),
                    ResponsePacketType.SNR => ParseSNR(hexString),
                    ResponsePacketType.Vol => ParseVol(hexString),
                    ResponsePacketType.Model => ParseModel(hexString),
                    ResponsePacketType.RadioVersion => ParseRadioVersion(hexString),
                    ResponsePacketType.EqualizerSettings => ParseEqualizerSettings(hexString),
                    ResponsePacketType.Heartbeat => ParseHeartbeat(hexString),
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
        // AB02 messages can be ignored per requirements
        _logger.LogDebug("AB02 (StatusShort) message - ignoring per requirements");
        return new { RawData = hexData, Type = "StatusShort" };
    }

    private object ParseFreqData1(string hexData)
    {
        // AB05 messages are time/alarm related - log and ignore per requirements
        _logger.LogDebug("AB05 (Time/Alarm) message - ignoring per requirements");
        return new { RawData = hexData, Type = "TimeAlarm" };
    }

    private object ParseFreqData2(string hexData)
    {
        // Check if this is the heartbeat message (AB061C) - handled separately
        if (hexData.StartsWith("ab061c"))
        {
            return ParseHeartbeat(hexData);
        }
        
        // Other AB06 messages can be ignored per requirements
        _logger.LogDebug("AB06 message (non-heartbeat) - ignoring per requirements");
        return new { RawData = hexData, Type = "FreqData2" };
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

    private object ParseBandwidth(string hexData)
    {
        // AB0D1C format (new detailed bandwidth):
        // AB0D1C - Header (6 chars)
        // Next 2 Bytes: Bandwidth Values (4 chars)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Bandwidth Text
        // Next Byte: Checksum (2 chars)
        
        // Check if this is the new AB0D1C format
        if (hexData.StartsWith("ab0d1c") && hexData.Length >= 14)
        {
            try
            {
                var value = Convert.ToInt32(hexData[6..10], 16);
                var textLength = Convert.ToByte(hexData[10..12], 16);
                
                var startIndex = 12;
                var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
                
                string? text = null;
                if (endIndex > startIndex)
                {
                    var textHex = hexData[startIndex..endIndex];
                    text = HexToAscii(textHex);
                }
                
                _logger.LogDebug($"Bandwidth: Value={value}, Text={text}");
                
                return new BandwidthInfo
                {
                    Value = value,
                    Text = text,
                    RawData = hexData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing bandwidth");
                return new BandwidthInfo { RawData = hexData };
            }
        }
        
        // Old format - return ModulationInfo for backward compatibility
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

    private TextMessageInfo ParseTextMessage(string hexData)
    {
        // AB11 text messages format:
        // AB1109 - Header (6 chars)
        // Next Byte: Message part indicator (01=start, 02=middle, 04=end)
        // Next Byte: Text Length
        // Next N Bytes: ASCII Message In Hex Format
        // Next Byte: Checksum
        
        if (hexData.Length < 10)
        {
            _logger.LogWarning("Text message packet too short");
            return new TextMessageInfo { RawData = hexData };
        }

        try
        {
            var partIndicator = hexData.Length > 7 ? Convert.ToByte(hexData[6..8], 16) : (byte)0;
            var textLength = hexData.Length > 9 ? Convert.ToByte(hexData[8..10], 16) : (byte)0;
            
            var messageKey = "text_message";
            var startIndex = 10;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2); // -2 for checksum
            
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                var messageText = HexToAscii(textHex);
                
                // Get or create multi-part message
                if (!_multiPartMessages.TryGetValue(messageKey, out var multiPart))
                {
                    multiPart = new MultiPartMessage
                    {
                        MessageType = "TextMessage",
                        SequenceId = messageKey,
                        StartTime = DateTime.Now
                    };
                }
                
                var parts = new List<string>(multiPart.ReceivedParts) { messageText };
                var updatedMessage = multiPart with
                {
                    PartialData = string.Concat(parts),
                    ReceivedParts = parts,
                    LastUpdate = DateTime.Now,
                    IsComplete = partIndicator == 0x04
                };
                
                _multiPartMessages[messageKey] = updatedMessage;
                
                if (updatedMessage.IsComplete)
                {
                    _logger.LogInfo($"Text message complete: {updatedMessage.PartialData}");
                    _multiPartMessages.Remove(messageKey);
                    
                    return new TextMessageInfo
                    {
                        Message = updatedMessage.PartialData,
                        IsComplete = true,
                        RawData = hexData
                    };
                }
                
                _logger.LogDebug($"Text message part received (indicator: 0x{partIndicator:X2}): {messageText}");
                return new TextMessageInfo
                {
                    Message = messageText,
                    IsComplete = false,
                    RawData = hexData
                };
            }
            
            return new TextMessageInfo { RawData = hexData };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing text message");
            return new TextMessageInfo { RawData = hexData };
        }
    }

    private DemodulationInfo ParseDemodulation(string hexData)
    {
        // AB101C format:
        // AB101C - Header (6 chars)
        // Next 2 Bytes: Demodulation Values (4 chars)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Demodulation Text
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("Demodulation packet too short");
            return new DemodulationInfo { RawData = hexData };
        }

        try
        {
            var value = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
            }
            
            _logger.LogDebug($"Demodulation: Value={value}, Text={text}");
            
            return new DemodulationInfo
            {
                Value = value,
                Text = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing demodulation");
            return new DemodulationInfo { RawData = hexData };
        }
    }


    private SNRInfo ParseSNR(string hexData)
    {
        // AB081C format:
        // AB081C - Header (6 chars)
        // Next 2 Bytes: SNR Values (4 chars)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: SNR Text
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("SNR packet too short");
            return new SNRInfo { RawData = hexData };
        }

        try
        {
            var value = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
            }
            
            _logger.LogDebug($"SNR: Value={value}, Text={text}");
            
            return new SNRInfo
            {
                Value = value,
                Text = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing SNR");
            return new SNRInfo { RawData = hexData };
        }
    }

    private VolInfo ParseVol(string hexData)
    {
        // AB071C format (note: issue says AB0D1C but that's bandwidth, this should be AB07)
        // AB071C - Header (6 chars)
        // Next 2 Bytes: Vol Values (4 chars)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Vol Text
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("Vol packet too short");
            return new VolInfo { RawData = hexData };
        }

        try
        {
            var value = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
            }
            
            _logger.LogDebug($"Vol: Value={value}, Text={text} (logged but not setting state per requirements)");
            
            return new VolInfo
            {
                Value = value,
                Text = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing vol");
            return new VolInfo { RawData = hexData };
        }
    }

    private ModelInfo ParseModel(string hexData)
    {
        // AB111C format:
        // AB111C - Header (6 chars)
        // Next 2 Bytes: Version Number (4 chars, binary)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Version Text (should replace ?? with version number)
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("Model packet too short");
            return new ModelInfo { RawData = hexData };
        }

        try
        {
            var versionNumber = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
                
                // Replace ?? with version number if present
                if (text != null && text.Contains("??"))
                {
                    text = text.Replace("??", versionNumber.ToString());
                }
            }
            
            _logger.LogInfo($"Model: Version={versionNumber}, Text={text}");
            
            return new ModelInfo
            {
                VersionNumber = versionNumber,
                VersionText = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing model");
            return new ModelInfo { RawData = hexData };
        }
    }

    private RadioVersionInfo ParseRadioVersion(string hexData)
    {
        // AB091C format:
        // AB091C - Header (6 chars)
        // Next 2 Bytes: Version Number (4 chars, binary)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Version Text
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("Radio version packet too short");
            return new RadioVersionInfo { RawData = hexData };
        }

        try
        {
            var versionNumber = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
            }
            
            _logger.LogInfo($"Radio Version: Version={versionNumber}, Text={text}");
            
            return new RadioVersionInfo
            {
                VersionNumber = versionNumber,
                VersionText = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing radio version");
            return new RadioVersionInfo { RawData = hexData };
        }
    }

    private EqualizerInfo ParseEqualizerSettings(string hexData)
    {
        // AB0C1C format:
        // AB0C1C - Header (6 chars)
        // Next 2 Bytes: Equalizer Info (4 chars)
        // Next Byte: Text Length (2 chars)
        // Next N Bytes: Equalization Type
        // Next Byte: Checksum (2 chars)
        
        if (hexData.Length < 14)
        {
            _logger.LogWarning("Equalizer settings packet too short");
            return new EqualizerInfo { RawData = hexData };
        }

        try
        {
            var value = Convert.ToInt32(hexData[6..10], 16);
            var textLength = Convert.ToByte(hexData[10..12], 16);
            
            var startIndex = 12;
            var endIndex = Math.Min(startIndex + (textLength * 2), hexData.Length - 2);
            
            string? text = null;
            var eqType = EqualizerType.Unknown;
            
            if (endIndex > startIndex)
            {
                var textHex = hexData[startIndex..endIndex];
                text = HexToAscii(textHex);
                
                // Parse equalizer type from text
                if (text != null)
                {
                    eqType = text.ToUpperInvariant() switch
                    {
                        var s when s.Contains("NORMAL") => EqualizerType.Normal,
                        var s when s.Contains("POP") => EqualizerType.Pop,
                        var s when s.Contains("ROCK") => EqualizerType.Rock,
                        var s when s.Contains("JAZZ") => EqualizerType.Jazz,
                        var s when s.Contains("CLASSIC") => EqualizerType.Classic,
                        var s when s.Contains("COUNTRY") => EqualizerType.Country,
                        _ => EqualizerType.Unknown
                    };
                }
            }
            
            _logger.LogInfo($"Equalizer: Type={eqType}, Value={value}, Text={text}");
            
            return new EqualizerInfo
            {
                Value = value,
                EqualizerType = eqType,
                Text = text,
                RawData = hexData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing equalizer settings");
            return new EqualizerInfo { RawData = hexData };
        }
    }

    private object ParseHeartbeat(string hexData)
    {
        // AB061C is a heartbeat message - just log and return
        _logger.LogDebug("Heartbeat received");
        return new { Type = "Heartbeat", Timestamp = DateTime.Now, RawData = hexData };
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