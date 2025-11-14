using FluentAssertions;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Protocol;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Protocol;

/// <summary>
/// Comprehensive tests for core radio status parsing: frequency, volume, band, and signal strength
/// </summary>
public class RadioStatusParsingTests
{
    private readonly MockRadioLogger _logger;
    private readonly RadioProtocolParser _parser;

    public RadioStatusParsingTests()
    {
        _logger = new MockRadioLogger();
        _parser = new RadioProtocolParser(_logger);
    }

    #region Frequency Status Tests

    [Fact]
    public void ParseReceivedData_WithFrequencyStatus_ShouldParseBasicStructure()
    {
        // Arrange - AB0417 frequency status packet
        var hexData = "ab0417010203040506070809";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        result.CommandId.Should().Be("ab0417");
        result.ParsedData.Should().BeOfType<RadioStatus>();
        
        var status = result.ParsedData as RadioStatus;
        status.Should().NotBeNull();
        status!.RawData.Should().Be(hexData);
    }

    [Theory]
    [InlineData("ab041700000000", 0)]
    [InlineData("ab041701000000", 1)]
    [InlineData("ab041702000000", 2)]
    [InlineData("ab041710000000", 16)]
    [InlineData("ab0417ff000000", 255)]
    public void ParseReceivedData_WithFrequencyStatus_ShouldHandleVariousStatusBytes(string hexData, int expectedByte)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
    }

    [Fact]
    public void ParseReceivedData_WithTooShortFrequencyStatus_ShouldStillHandleGracefully()
    {
        // Arrange - Packet shorter than expected but still minimum length
        var hexData = "ab04170102";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        // May not have parsed data if too short, but should not crash
    }

    #endregion

    #region Band Info Tests

    [Fact]
    public void ParseReceivedData_WithBandInfo_ShouldParseBandCode()
    {
        // Arrange - AB0901 band info packet with band code 06 (VHF)
        var hexData = "ab09010601020304050607080910111213141516171819";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.BandInfo);
        result.ParsedData.Should().BeOfType<FrequencyInfo>();
        
        var freqInfo = result.ParsedData as FrequencyInfo;
        freqInfo!.BandCode.Should().Be("06");
    }

    [Theory]
    [InlineData("ab090100", "00")]  // FM band
    [InlineData("ab090101", "01")]  // MW band
    [InlineData("ab090102", "02")]  // SW band
    [InlineData("ab090106", "06")]  // VHF band
    [InlineData("ab09010a", "0a")]  // Custom band
    public void ParseReceivedData_WithBandInfo_ShouldHandleDifferentBands(string hexPrefix, string expectedBandCode)
    {
        // Arrange - Add dummy sub-band data
        var hexData = hexPrefix + "01020304050607080910111213141516171819";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.BandInfo);
        result.ParsedData.Should().BeOfType<FrequencyInfo>();
        
        var freqInfo = result.ParsedData as FrequencyInfo;
        freqInfo!.BandCode.Should().Be(expectedBandCode);
    }

    [Fact]
    public void ParseReceivedData_WithBandInfo_ShouldParseSubBands()
    {
        // Arrange - Band info with specific sub-band values
        var hexData = "ab0901061a2b3c4d5e6f7081920a0b0c0d0e0f";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.BandInfo);
        result.ParsedData.Should().BeOfType<FrequencyInfo>();
        
        var freqInfo = result.ParsedData as FrequencyInfo;
        freqInfo!.BandCode.Should().Be("06");
        freqInfo.SubBand1.Should().Be("1a");
        freqInfo.SubBand2.Should().Be("2b");
        freqInfo.SubBand3.Should().Be("3c");
        freqInfo.SubBand4.Should().Be("4d");
    }

    [Fact]
    public void ParseReceivedData_WithTooShortBandInfo_ShouldHandleGracefully()
    {
        // Arrange - Band info packet too short but still has band code
        var hexData = "ab09010601020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.BandInfo);
        result.ParsedData.Should().BeOfType<FrequencyInfo>();
    }

    #endregion

    #region Volume Tests

    [Fact]
    public void ParseReceivedData_WithVolumeMessage_ShouldIdentifyCorrectly()
    {
        // Arrange - AB0303 volume packet (need 6+ bytes = 12+ hex chars)
        var hexData = "ab0303081516";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Volume);
        result.ParsedData.Should().BeOfType<AudioInfo>();
    }

    [Theory]
    [InlineData("ab030300101112")]  // Volume 0
    [InlineData("ab030308111213")]  // Volume 8
    [InlineData("ab03030f121314")]  // Volume 15 (max)
    [InlineData("ab030310131415")]  // Volume 16 (beyond typical max)
    public void ParseReceivedData_WithVolumeMessage_ShouldHandleDifferentLevels(string hexData)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.Volume);
        result.ParsedData.Should().BeOfType<AudioInfo>();
    }

    [Fact]
    public void ParseReceivedData_WithDetailedFreq_AB09_ShouldParse()
    {
        // Arrange - AB09 detailed frequency packet (not AB0901)
        // Must be AB09 followed by something other than 01
        var hexData = "ab09020304050607";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.DetailedFreq);
    }

    #endregion

    #region Signal Strength Tests

    [Fact]
    public void ParseReceivedData_WithSignalMessage_ShouldIdentifyCorrectly()
    {
        // Arrange - AB031F signal strength packet (need 6+ bytes)
        var hexData = "ab031f050607";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Signal);
        result.ParsedData.Should().BeOfType<AudioInfo>();
    }

    [Theory]
    [InlineData("ab031f001011")]  // No signal
    [InlineData("ab031f011112")]  // Weak signal
    [InlineData("ab031f031213")]  // Medium signal
    [InlineData("ab031f051314")]  // Strong signal
    [InlineData("ab031f061415")]  // Max signal
    public void ParseReceivedData_WithSignalMessage_ShouldHandleDifferentStrengths(string hexData)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.Signal);
        result.ParsedData.Should().BeOfType<AudioInfo>();
    }

    #endregion

    #region Battery Tests

    [Fact]
    public void ParseReceivedData_WithBatteryMessage_ShouldIdentifyCorrectly()
    {
        // Arrange - AB07 battery packet (but NOT AB071C which is Vol)
        var hexData = "ab0704050607";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Battery);
        result.ParsedData.Should().BeOfType<BatteryInfo>();
    }

    [Theory]
    [InlineData("ab070001020304")]  // Empty battery
    [InlineData("ab073202030405")]  // 50% battery
    [InlineData("ab076403040506")]  // Full battery
    [InlineData("ab07ff04050607")]  // Max value
    public void ParseReceivedData_WithBatteryMessage_ShouldHandleDifferentLevels(string hexData)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.Battery);
        result.ParsedData.Should().BeOfType<BatteryInfo>();
    }

    #endregion

    #region Time Tests

    [Fact]
    public void ParseReceivedData_WithTimeMessage_ShouldIdentifyCorrectly()
    {
        // Arrange - AB031E time packet
        var hexData = "ab031e0c1e2a3b";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.Time);
        result.ParsedData.Should().BeOfType<TimeInfo>();
    }

    #endregion

    #region Frequency Input Tests

    [Fact]
    public void ParseReceivedData_WithFrequencyInput_ShouldIdentifyCorrectly()
    {
        // Arrange - AB090F frequency input packet
        var hexData = "ab090f0102030405";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.FrequencyInput);
    }

    #endregion

    #region Sub-Band Tests

    [Fact]
    public void ParseReceivedData_WithSubBandInfo_ShouldIdentifyCorrectly()
    {
        // Arrange - AB0E sub-band info packet
        var hexData = "ab0e01020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.SubBandInfo);
    }

    #endregion

    #region Lock Status Tests

    [Fact]
    public void ParseReceivedData_WithLockStatus_ShouldIdentifyCorrectly()
    {
        // Arrange - AB08 lock status packet (but NOT AB081C which is SNR)
        var hexData = "ab0801020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.LockStatus);
    }

    [Theory]
    [InlineData("ab0800010203")]  // Unlocked
    [InlineData("ab0801020304")]  // Locked
    public void ParseReceivedData_WithLockStatus_ShouldHandleDifferentStates(string hexData)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.LockStatus);
    }

    #endregion

    #region Recording Status Tests

    [Fact]
    public void ParseReceivedData_WithRecordingStatus_ShouldIdentifyCorrectly()
    {
        // Arrange - AB0B recording status packet with proper length
        var hexData = "ab0b01020304050607";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.PacketType.Should().Be(ResponsePacketType.RecordingStatus);
        result.ParsedData.Should().BeOfType<RecordingInfo>();
    }

    [Theory]
    [InlineData("ab0b000102030405")]  // Not recording
    [InlineData("ab0b010203040506")]  // Recording
    public void ParseReceivedData_WithRecordingStatus_ShouldHandleDifferentStates(string hexData)
    {
        // Arrange
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.PacketType.Should().Be(ResponsePacketType.RecordingStatus);
        result.ParsedData.Should().BeOfType<RecordingInfo>();
    }

    #endregion

    #region Integration Tests - Multiple Status Messages

    [Fact]
    public void ParseReceivedData_WithMultipleFrequencyMessages_ShouldMaintainIndependence()
    {
        // Arrange
        var hexData1 = "ab041701020304";
        var hexData2 = "ab0417050607080910";
        var data1 = Convert.FromHexString(hexData1);
        var data2 = Convert.FromHexString(hexData2);

        // Act
        var result1 = _parser.ParseReceivedData(data1);
        var result2 = _parser.ParseReceivedData(data2);

        // Assert
        result1.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        result2.PacketType.Should().Be(ResponsePacketType.FrequencyStatus);
        result1.HexData.Should().NotBe(result2.HexData);
    }

    [Fact]
    public void ParseReceivedData_WithSequenceOfStatusMessages_ShouldParseAllCorrectly()
    {
        // Arrange - Sequence of different status messages (all must be 6+ bytes)
        var messages = new[]
        {
            ("ab041701020304", ResponsePacketType.FrequencyStatus),
            ("ab03030f1011", ResponsePacketType.Volume),
            ("ab031f051112", ResponsePacketType.Signal),
            ("ab090106010203", ResponsePacketType.BandInfo),
            ("ab0764040506", ResponsePacketType.Battery)
        };

        // Act & Assert
        foreach (var (hexData, expectedType) in messages)
        {
            var data = Convert.FromHexString(hexData);
            var result = _parser.ParseReceivedData(data);
            
            result.Should().NotBeNull();
            result.PacketType.Should().Be(expectedType, $"Failed for {hexData}");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseReceivedData_WithMinimumValidLength_ShouldParse()
    {
        // Arrange - Minimum 6-byte packet (3 bytes = 6 hex chars)
        var hexData = "ab0401020304";
        var data = Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("ab04")]
    [InlineData("ab0401")]
    public void ParseReceivedData_WithBelowMinimumLength_ShouldReturnInvalid(string hexData)
    {
        // Arrange
        var data = string.IsNullOrEmpty(hexData) ? Array.Empty<byte>() : Convert.FromHexString(hexData);

        // Act
        var result = _parser.ParseReceivedData(data);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid packet length");
    }

    #endregion
}
