using FluentAssertions;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Protocol;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Protocol;

/// <summary>
/// Tests for new message format parsers added for enhanced protocol support
/// </summary>
public class NewMessageParserTests
{
    private readonly MockRadioLogger _logger;
    private readonly RadioProtocolParser _parser;

    public NewMessageParserTests()
    {
        _logger = new MockRadioLogger();
        _parser = new RadioProtocolParser(_logger);
    }

    [Fact]
    public void ParseReceivedData_WithDemodulationMessage_ShouldParseCorrectly()
    {
        // Arrange - AB101C format with demodulation info
        // AB101C + Value (1234 = 0x04D2) + Length (03) + "NFM" in ASCII + Checksum
        var hexData = "ab101c04d2034e464d00";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Demodulation);
        result.ParsedData.Should().BeOfType<DemodulationInfo>();
        
        var demodInfo = result.ParsedData as DemodulationInfo;
        demodInfo!.Value.Should().Be(0x04D2);
        demodInfo.Text.Should().Be("NFM");
    }

    [Fact]
    public void ParseReceivedData_WithBandwidthMessage_ShouldParseCorrectly()
    {
        // Arrange - AB0D1C format with bandwidth info
        // AB0D1C + Value (2500 = 0x09C4) + Length (05) + "25kHz" in ASCII + Checksum
        var hexData = "ab0d1c09c405323520486300";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Bandwidth);
        result.ParsedData.Should().BeOfType<BandwidthInfo>();
        
        var bwInfo = result.ParsedData as BandwidthInfo;
        bwInfo!.Value.Should().Be(0x09C4);
        bwInfo.Text.Should().Contain("25");
    }

    [Fact]
    public void ParseReceivedData_WithSNRMessage_ShouldParseCorrectly()
    {
        // Arrange - AB081C format with SNR info
        // AB081C + Value (0050 = 80 decimal) + Length (02) + "80" in ASCII + Checksum
        var hexData = "ab081c0050023830ff";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.SNR);
        result.ParsedData.Should().BeOfType<SNRInfo>();
        
        var snrInfo = result.ParsedData as SNRInfo;
        snrInfo!.Value.Should().Be(80);
        snrInfo.Text.Should().Be("80");
    }

    [Fact]
    public void ParseReceivedData_WithVolMessage_ShouldParseCorrectly()
    {
        // Arrange - AB071C format with volume info
        // AB071C + Value (000F = 15) + Length (02) + "15" in ASCII + Checksum
        var hexData = "ab071c000f0231354d";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Vol);
        result.ParsedData.Should().BeOfType<VolInfo>();
        
        var volInfo = result.ParsedData as VolInfo;
        volInfo!.Value.Should().Be(15);
        volInfo.Text.Should().Be("15");
    }

    [Fact]
    public void ParseReceivedData_WithModelMessage_ShouldParseCorrectly()
    {
        // Arrange - AB111C format with model info
        // AB111C + Version (0320 = 800) + Length (0E) + "Model: ?? v1.0" + Checksum
        // where ?? should be replaced with version number
        var hexData = "ab111c03200e4d6f64656c3a203f3f2076312e3000";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Model);
        result.ParsedData.Should().BeOfType<ModelInfo>();
        
        var modelInfo = result.ParsedData as ModelInfo;
        modelInfo!.VersionNumber.Should().Be(800);
        modelInfo.VersionText.Should().Contain("Model");
        modelInfo.VersionText.Should().Contain("800"); // ?? replaced with version
    }

    [Fact]
    public void ParseReceivedData_WithRadioVersionMessage_ShouldParseCorrectly()
    {
        // Arrange - AB091C format with radio version info
        // AB091C + Version (0102 = 258) + Length (0C) + "Version 1.02" + Checksum
        var hexData = "ab091c01020c56657273696f6e20312e303200";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.RadioVersion);
        result.ParsedData.Should().BeOfType<RadioVersionInfo>();
        
        var versionInfo = result.ParsedData as RadioVersionInfo;
        versionInfo!.VersionNumber.Should().Be(258);
        versionInfo.VersionText.Should().Contain("Version");
    }

    [Theory]
    [InlineData("EQ: NORMAL", EqualizerType.Normal)]
    [InlineData("EQ: POP", EqualizerType.Pop)]
    [InlineData("EQ: ROCK", EqualizerType.Rock)]
    [InlineData("EQ: JAZZ", EqualizerType.Jazz)]
    [InlineData("EQ: CLASSIC", EqualizerType.Classic)]
    [InlineData("EQ: COUNTRY", EqualizerType.Country)]
    public void ParseReceivedData_WithEqualizerMessage_ShouldParseCorrectly(string eqText, EqualizerType expectedType)
    {
        // Arrange - AB0C1C format with equalizer info
        var textBytes = System.Text.Encoding.ASCII.GetBytes(eqText);
        var textHex = Convert.ToHexString(textBytes).ToLowerInvariant();
        var lengthHex = textBytes.Length.ToString("x2");
        var hexData = $"ab0c1c0000{lengthHex}{textHex}ff";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.EqualizerSettings);
        result.ParsedData.Should().BeOfType<EqualizerInfo>();
        
        var eqInfo = result.ParsedData as EqualizerInfo;
        eqInfo!.EqualizerType.Should().Be(expectedType);
        eqInfo.Text.Should().Be(eqText);
    }

    [Fact]
    public void ParseReceivedData_WithHeartbeatMessage_ShouldIdentifyCorrectly()
    {
        // Arrange - AB061C heartbeat message
        var hexData = "ab061c000000ff";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Heartbeat);
    }

    [Fact]
    public void ParseReceivedData_WithTextMessageStart_ShouldParsePartially()
    {
        // Arrange - AB1109 text message start
        // AB1109 + Part Indicator (01 = start) + Length (05) + "Hello" + Checksum
        var hexData = "ab1109010548656c6c6fff";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.TextMessage);
        result.ParsedData.Should().BeOfType<TextMessageInfo>();
        
        var textInfo = result.ParsedData as TextMessageInfo;
        textInfo!.Message.Should().Be("Hello");
        textInfo.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void ParseReceivedData_WithTextMessageEnd_ShouldMarkComplete()
    {
        // Arrange - AB1109 text message end
        // AB1109 + Part Indicator (04 = end) + Length (06) + " World" + Checksum
        var hexData = "ab1109040620576f726c64ff";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.TextMessage);
        result.ParsedData.Should().BeOfType<TextMessageInfo>();
        
        var textInfo = result.ParsedData as TextMessageInfo;
        textInfo!.IsComplete.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab02", ResponsePacketType.StatusShort)]
    [InlineData("ab05", ResponsePacketType.FreqData1)]
    [InlineData("ab06", ResponsePacketType.FreqData2)]
    public void ParseReceivedData_WithIgnoredMessages_ShouldStillParse(string prefix, ResponsePacketType expectedType)
    {
        // Arrange - Messages that should be ignored per requirements
        var hexData = prefix + "01020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(expectedType);
    }
}
