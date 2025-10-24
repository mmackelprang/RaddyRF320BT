using FluentAssertions;
using RadioProtocol.Core.Commands;
using RadioProtocol.Core.Constants;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Commands;

/// <summary>
/// Tests for RadioCommandBuilder
/// </summary>
public class RadioCommandBuilderTests
{
    private readonly MockRadioLogger _logger;
    private readonly RadioCommandBuilder _commandBuilder;

    public RadioCommandBuilderTests()
    {
        _logger = new MockRadioLogger();
        _commandBuilder = new RadioCommandBuilder(_logger);
    }

    [Fact]
    public void BuildButtonCommand_ShouldCreateValidCommand()
    {
        // Arrange
        var buttonType = ButtonType.Number1;

        // Act
        var command = _commandBuilder.BuildButtonCommand(buttonType);

        // Assert
        command.Should().HaveCount(ProtocolConstants.COMMAND_PACKET_SIZE);
        command[0].Should().Be(ProtocolConstants.PROTOCOL_START_BYTE);
        command[1].Should().Be(ProtocolConstants.MESSAGE_LENGTH_STANDARD);
        command[2].Should().Be(ProtocolConstants.COMMAND_TYPE_BUTTON);
        command[3].Should().Be((byte)buttonType);
        
        // Verify checksum
        var expectedChecksum = (command[0] + command[1] + command[2] + command[3]) & 0xFF;
        command[4].Should().Be((byte)expectedChecksum);
    }

    [Fact]
    public void BuildHandshakeCommand_ShouldCreateValidHandshake()
    {
        // Act
        var command = _commandBuilder.BuildHandshakeCommand();

        // Assert
        command.Should().HaveCount(4);
        command[0].Should().Be(ProtocolConstants.PROTOCOL_START_BYTE);
        command[1].Should().Be(ProtocolConstants.MESSAGE_LENGTH_HANDSHAKE);
        command[2].Should().Be(ProtocolConstants.DATA_HANDSHAKE);
        command[3].Should().Be(ProtocolConstants.PROTOCOL_START_BYTE);
    }

    [Theory]
    [InlineData(ButtonType.Number0, 0x0A)]
    [InlineData(ButtonType.Number1, 0x01)]
    [InlineData(ButtonType.Number9, 0x09)]
    [InlineData(ButtonType.VolumeUp, 0x12)]
    [InlineData(ButtonType.VolumeDown, 0x13)]
    [InlineData(ButtonType.Power, 0x14)]
    public void BuildButtonCommand_ShouldUseCorrectDataByte(ButtonType buttonType, byte expectedDataByte)
    {
        // Act
        var command = _commandBuilder.BuildButtonCommand(buttonType);

        // Assert
        command[3].Should().Be(expectedDataByte);
    }

    [Fact]
    public void CalculateChecksum_ShouldReturnCorrectValue()
    {
        // Arrange
        var data = new byte[] { 0xAB, 0x02, 0x0C, 0x01 };

        // Act
        var checksum = RadioCommandBuilder.CalculateChecksum(data);

        // Assert
        var expected = (0xAB + 0x02 + 0x0C + 0x01) & 0xFF;
        checksum.Should().Be((byte)expected);
    }

    [Fact]
    public void VerifyChecksum_WithValidChecksum_ShouldReturnTrue()
    {
        // Arrange
        var command = new byte[] { 0xAB, 0x02, 0x0C, 0x01, 0xBA };

        // Act
        var isValid = RadioCommandBuilder.VerifyChecksum(command);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChecksum_WithInvalidChecksum_ShouldReturnFalse()
    {
        // Arrange
        var command = new byte[] { 0xAB, 0x02, 0x0C, 0x01, 0x00 };

        // Act
        var isValid = RadioCommandBuilder.VerifyChecksum(command);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void BuildCommand_ShouldLogMessage()
    {
        // Arrange
        var buttonType = ButtonType.VolumeUp;

        // Act
        _commandBuilder.BuildButtonCommand(buttonType);

        // Assert
        _logger.MessagesSent.Should().HaveCount(1);
        _logger.MessagesSent[0].messageType.Should().Be("ButtonPress");
    }
}

/// <summary>
/// Tests for CommonCommands static class
/// </summary>
public class CommonCommandsTests
{
    private readonly MockRadioLogger _logger;

    public CommonCommandsTests()
    {
        _logger = new MockRadioLogger();
    }

    [Theory]
    [InlineData(0, ButtonType.Number0)]
    [InlineData(1, ButtonType.Number1)]
    [InlineData(5, ButtonType.Number5)]
    [InlineData(9, ButtonType.Number9)]
    public void NumberButton_ShouldReturnCorrectCommand(int number, ButtonType expectedButtonType)
    {
        // Act
        var command = CommonCommands.NumberButton(number, _logger);

        // Assert
        command[3].Should().Be((byte)expectedButtonType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(99)]
    public void NumberButton_WithInvalidNumber_ShouldThrowException(int invalidNumber)
    {
        // Act & Assert
        Action act = () => CommonCommands.NumberButton(invalidNumber, _logger);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VolumeUp_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Volume.Up(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.VolumeUp);
    }

    [Fact]
    public void VolumeDown_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Volume.Down(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.VolumeDown);
    }

    [Fact]
    public void NavigationUp_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Navigation.Up(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.UpShort);
    }

    [Fact]
    public void NavigationDown_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Navigation.Down(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.DownShort);
    }

    [Fact]
    public void PowerToggle_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Power.Toggle(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.Power);
    }

    [Fact]
    public void PowerOff_ShouldReturnCorrectCommand()
    {
        // Act
        var command = CommonCommands.Power.Off(_logger);

        // Assert
        command[3].Should().Be((byte)ButtonType.PowerLong);
    }
}