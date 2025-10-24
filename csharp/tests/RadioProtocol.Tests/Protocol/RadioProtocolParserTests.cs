using FluentAssertions;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Protocol;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Protocol;

/// <summary>
/// Tests for RadioProtocolParser based on documented message formats
/// </summary>
public class RadioProtocolParserTests
{
    private readonly MockRadioLogger _logger;
    private readonly RadioProtocolParser _parser;

    public RadioProtocolParserTests()
    {
        _logger = new MockRadioLogger();
        _parser = new RadioProtocolParser(_logger);
    }

    [Fact]
    public void ParseReceivedData_WithValidFrequencyStatus_ShouldReturnParsedPacket()
    {
        // Arrange - Based on documented ab0417 packet
        var hexData = "ab041701020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        result.CommandId.Should().Be("ab0417");
        result.ParsedData.Should().BeOfType<RadioStatus>();
    }

    [Fact]
    public void ParseReceivedData_WithValidBandInfo_ShouldReturnParsedPacket()
    {
        // Arrange - Based on documented ab0901 packet
        var hexData = "ab090106010203040506070809101112131415161718192021";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.BandInfo);
        result.CommandId.Should().Be("ab0901");
        result.ParsedData.Should().BeOfType<FrequencyInfo>();
        
        var frequencyInfo = result.ParsedData as FrequencyInfo;
        frequencyInfo!.BandCode.Should().Be("06");
        frequencyInfo.SubBand1.Should().Be("01");
        frequencyInfo.SubBand2.Should().Be("02");
        frequencyInfo.SubBand3.Should().Be("03");
        frequencyInfo.SubBand4.Should().Be("04");
    }

    [Fact]
    public void ParseReceivedData_WithDeviceInfoPacket_ShouldParseAsciiText()
    {
        // Arrange - Based on documented AB11 device info packet
        // "Radio version " in ASCII = 526164696F2076657273696F6E20
        var hexData = "ab1119010e526164696f2076657273696f6e2019";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.DeviceInfo);
        result.ParsedData.Should().BeOfType<DeviceInfo>();
        
        var deviceInfo = result.ParsedData as DeviceInfo;
        deviceInfo!.RadioVersion.Should().Contain("Radio version");
    }

    [Theory]
    [InlineData("ab031e", ResponsePacketType.Time)]
    [InlineData("ab0303", ResponsePacketType.Volume)]
    [InlineData("ab031f", ResponsePacketType.Signal)]
    [InlineData("ab090f", ResponsePacketType.FrequencyInput)]
    [InlineData("ab10", ResponsePacketType.DeviceInfoCont)]
    [InlineData("ab0e", ResponsePacketType.SubBandInfo)]
    [InlineData("ab08", ResponsePacketType.LockStatus)]
    [InlineData("ab0b", ResponsePacketType.RecordingStatus)]
    [InlineData("ab02", ResponsePacketType.StatusShort)]
    [InlineData("ab05", ResponsePacketType.FreqData1)]
    [InlineData("ab06", ResponsePacketType.FreqData2)]
    [InlineData("ab07", ResponsePacketType.Battery)]
    [InlineData("ab09", ResponsePacketType.DetailedFreq)]
    [InlineData("ab0d", ResponsePacketType.Bandwidth)]
    public void ParseReceivedData_WithKnownCommandTypes_ShouldIdentifyCorrectType(string commandPrefix, ResponsePacketType expectedType)
    {
        // Arrange
        var hexData = commandPrefix + "01020304050607080910"; // Add some dummy data
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.PacketType.Should().Be(expectedType);
        result.CommandId.Should().StartWith(commandPrefix);
    }

    [Fact]
    public void ParseReceivedData_WithUnknownCommandType_ShouldReturnUnknownType()
    {
        // Arrange
        var hexData = "ab9999010203"; // Unknown command type
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.PacketType.Should().Be(ResponsePacketType.Unknown);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ParseReceivedData_WithInvalidData_ShouldReturnInvalidPacket()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02 }; // Too short

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid packet length");
    }

    [Fact]
    public void ParseReceivedData_WithNullData_ShouldReturnInvalidPacket()
    {
        // Act
        var result = _parser.ParseReceivedData(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid packet length");
    }

    [Fact]
    public void ParseReceivedData_ShouldLogRawDataReceived()
    {
        // Arrange
        var data = Convert.FromHexString("ab0417010203");

        // Act
        _parser.ParseReceivedData(data);

        // Assert
        _logger.RawDataReceived.Should().HaveCount(1);
        _logger.RawDataReceived[0].data.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void ParseReceivedData_WithValidParsedData_ShouldLogMessageReceived()
    {
        // Arrange
        var data = Convert.FromHexString("ab0417010203040506");

        // Act
        _parser.ParseReceivedData(data);

        // Assert
        _logger.MessagesReceived.Should().HaveCount(1);
        _logger.MessagesReceived[0].messageType.Should().Be("FrequencyStatus");
    }

    [Fact]
    public void BytesToHexString_ShouldReturnLowercaseHex()
    {
        // Arrange
        var data = new byte[] { 0xAB, 0x01, 0xFF };

        // Act
        var result = RadioProtocolParser.BytesToHexString(data);

        // Assert
        result.Should().Be("ab01ff");
    }
}

/// <summary>
/// Integration tests using real message data from documentation
/// </summary>
public class DocumentedMessageTests
{
    private readonly MockRadioLogger _logger;
    private readonly RadioProtocolParser _parser;

    public DocumentedMessageTests()
    {
        _logger = new MockRadioLogger();
        _parser = new RadioProtocolParser(_logger);
    }

    [Fact]
    public void ParseHandshakeResponse_FromDocumentedSequence_ShouldParseCorrectly()
    {
        // Arrange - From COMMAND_RESPONSE_SEQUENCES.md Frame 832
        var handshakeResponseData = "AB0220"; // Status (short format)
        var data = Convert.FromHexString(handshakeResponseData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.PacketType.Should().Be(ResponsePacketType.StatusShort);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ParseBatteryResponse_FromDocumentedSequence_ShouldParseCorrectly()
    {
        // Arrange - From COMMAND_RESPONSE_SEQUENCES.md Frame 833
        var batteryData = "AB0417"; // Battery level (detailed) - adding minimal data for valid packet
        var fullData = batteryData + "01020304050607080910";
        var data = Convert.FromHexString(fullData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("AB0E21", ResponsePacketType.SubBandInfo)]    // Frame 834
    [InlineData("AB0821", ResponsePacketType.LockStatus)]     // Frame 835
    [InlineData("AB0B1C", ResponsePacketType.RecordingStatus)] // Frame 836
    [InlineData("AB0205", ResponsePacketType.StatusShort)]     // Frame 837
    [InlineData("AB0506", ResponsePacketType.FreqData1)]       // Frame 838
    [InlineData("AB0308", ResponsePacketType.Volume)]          // Frame 839
    public void ParseDocumentedResponses_ShouldIdentifyCorrectTypes(string hexPrefix, ResponsePacketType expectedType)
    {
        // Arrange - Add minimal data to make valid packets
        var fullHex = hexPrefix + "010203040506070809";
        var data = Convert.FromHexString(fullHex);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.PacketType.Should().Be(expectedType);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ParseMultiPartDeviceInfo_ShouldHandleAsciiContent()
    {
        // Arrange - Multi-part device info from ADDITIONAL_MESSAGES.md
        var messages = new[]
        {
            "AB1119010E526164696F2076657273696F6E2019",  // "Radio version "
            "AB1119020E3A2056342E300A4D6F64656C203A7C",   // ": V4.0\nModel :"
            "AB111903104B3332333456332D34000A0070", // Continue with more device info
        };

        foreach (var hexMessage in messages)
        {
            var data = Convert.FromHexString(hexMessage);
            
            // Act
            var result = _parser.ParseReceivedData(data);

            // Assert
            result.PacketType.Should().Be(ResponsePacketType.DeviceInfo);
            result.IsValid.Should().BeTrue();
            result.ParsedData.Should().BeOfType<DeviceInfo>();
        }
    }
}