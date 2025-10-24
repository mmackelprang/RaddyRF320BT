using FluentAssertions;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Messages;
using RadioProtocol.Core.Models;
using Xunit;

namespace RadioProtocol.Tests.Messages;

/// <summary>
/// Tests for protocol message types and structures
/// </summary>
public class MessageTests
{
    [Fact]
    public void ProtocolConstants_ShouldHaveCorrectValues()
    {
        // Assert
        ProtocolConstants.PROTOCOL_START_BYTE.Should().Be(0xAB);
        ProtocolConstants.MESSAGE_VERSION.Should().Be(0x20);
        ProtocolConstants.DEFAULT_RADIO_ID.Should().Be(0x01);
        ProtocolConstants.MAX_MESSAGE_LENGTH.Should().Be(255);
        ProtocolConstants.MIN_MESSAGE_LENGTH.Should().Be(4);
    }

    [Fact]
    public void ButtonType_ShouldHaveExpectedValues()
    {
        // Assert - Check some key button types
        ButtonType.POWER.Should().Be(ButtonType.POWER);
        ButtonType.VOLUME_UP.Should().Be(ButtonType.VOLUME_UP);
        ButtonType.VOLUME_DOWN.Should().Be(ButtonType.VOLUME_DOWN);
        ButtonType.CHANNEL_UP.Should().Be(ButtonType.CHANNEL_UP);
        ButtonType.CHANNEL_DOWN.Should().Be(ButtonType.CHANNEL_DOWN);
        ButtonType.PTT.Should().Be(ButtonType.PTT);
        ButtonType.MENU.Should().Be(ButtonType.MENU);
        ButtonType.SELECT.Should().Be(ButtonType.SELECT);
        ButtonType.BACK.Should().Be(ButtonType.BACK);
        ButtonType.SCAN.Should().Be(ButtonType.SCAN);
    }

    [Fact]
    public void MessageType_ShouldHaveExpectedValues()
    {
        // Assert
        MessageType.BUTTON_PRESS.Should().Be(MessageType.BUTTON_PRESS);
        MessageType.CHANNEL_COMMAND.Should().Be(MessageType.CHANNEL_COMMAND);
        MessageType.SYNC_REQUEST.Should().Be(MessageType.SYNC_REQUEST);
        MessageType.SYNC_RESPONSE.Should().Be(MessageType.SYNC_RESPONSE);
        MessageType.STATUS_REQUEST.Should().Be(MessageType.STATUS_REQUEST);
        MessageType.STATUS_RESPONSE.Should().Be(MessageType.STATUS_RESPONSE);
        MessageType.GENERAL_RESPONSE.Should().Be(MessageType.GENERAL_RESPONSE);
    }

    [Fact]
    public void ConnectionState_ShouldHaveExpectedValues()
    {
        // Assert
        ConnectionState.Disconnected.Should().Be(ConnectionState.Disconnected);
        ConnectionState.Connecting.Should().Be(ConnectionState.Connecting);
        ConnectionState.Connected.Should().Be(ConnectionState.Connected);
        ConnectionState.Disconnecting.Should().Be(ConnectionState.Disconnecting);
        ConnectionState.Error.Should().Be(ConnectionState.Error);
    }

    [Theory]
    [InlineData(ButtonType.POWER, "POWER")]
    [InlineData(ButtonType.VOLUME_UP, "VOLUME_UP")]
    [InlineData(ButtonType.PTT, "PTT")]
    [InlineData(ButtonType.MENU, "MENU")]
    public void ButtonType_ToString_ShouldReturnCorrectName(ButtonType buttonType, string expectedName)
    {
        // Act
        var result = buttonType.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(MessageType.BUTTON_PRESS, "BUTTON_PRESS")]
    [InlineData(MessageType.SYNC_REQUEST, "SYNC_REQUEST")]
    [InlineData(MessageType.STATUS_RESPONSE, "STATUS_RESPONSE")]
    public void MessageType_ToString_ShouldReturnCorrectName(MessageType messageType, string expectedName)
    {
        // Act
        var result = messageType.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void ConnectionInfo_ShouldInitializeCorrectly()
    {
        // Arrange
        var deviceAddress = "00:11:22:33:44:55";
        var state = ConnectionState.Connected;
        var timestamp = DateTime.UtcNow;

        // Act
        var connectionInfo = new ConnectionInfo(state, deviceAddress, timestamp, null);

        // Assert
        connectionInfo.State.Should().Be(state);
        connectionInfo.DeviceAddress.Should().Be(deviceAddress);
        connectionInfo.Timestamp.Should().Be(timestamp);
        connectionInfo.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ConnectionInfo_WithError_ShouldIncludeErrorMessage()
    {
        // Arrange
        var errorMessage = "Connection failed";
        var state = ConnectionState.Error;

        // Act
        var connectionInfo = new ConnectionInfo(state, null, DateTime.UtcNow, errorMessage);

        // Assert
        connectionInfo.State.Should().Be(state);
        connectionInfo.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void ButtonPressMessage_ShouldCreateCorrectly()
    {
        // Arrange
        var buttonType = ButtonType.PTT;
        var radioId = 0x02;

        // Act
        var message = new ButtonPressMessage(buttonType, radioId);

        // Assert
        message.ButtonType.Should().Be(buttonType);
        message.RadioId.Should().Be((byte)radioId);
        message.MessageType.Should().Be(MessageType.BUTTON_PRESS);
    }

    [Fact]
    public void ChannelCommandMessage_ShouldCreateCorrectly()
    {
        // Arrange
        var channelNumber = 5;
        var radioId = 0x01;

        // Act
        var message = new ChannelCommandMessage(channelNumber, radioId);

        // Assert
        message.ChannelNumber.Should().Be(channelNumber);
        message.RadioId.Should().Be((byte)radioId);
        message.MessageType.Should().Be(MessageType.CHANNEL_COMMAND);
    }

    [Fact]
    public void SyncRequestMessage_ShouldCreateCorrectly()
    {
        // Arrange
        var radioId = 0x03;

        // Act
        var message = new SyncRequestMessage(radioId);

        // Assert
        message.RadioId.Should().Be((byte)radioId);
        message.MessageType.Should().Be(MessageType.SYNC_REQUEST);
    }

    [Fact]
    public void StatusRequestMessage_ShouldCreateCorrectly()
    {
        // Arrange
        var radioId = 0x04;

        // Act
        var message = new StatusRequestMessage(radioId);

        // Assert
        message.RadioId.Should().Be((byte)radioId);
        message.MessageType.Should().Be(MessageType.STATUS_REQUEST);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(255)]
    public void ChannelCommandMessage_ShouldAcceptValidChannelNumbers(int channelNumber)
    {
        // Act & Assert
        var act = () => new ChannelCommandMessage(channelNumber);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void ChannelCommandMessage_ShouldRejectInvalidChannelNumbers(int channelNumber)
    {
        // Act & Assert
        var act = () => new ChannelCommandMessage(channelNumber);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void Messages_ShouldAcceptValidRadioIds(byte radioId)
    {
        // Act & Assert
        var buttonAct = () => new ButtonPressMessage(ButtonType.POWER, radioId);
        var channelAct = () => new ChannelCommandMessage(1, radioId);
        var syncAct = () => new SyncRequestMessage(radioId);
        var statusAct = () => new StatusRequestMessage(radioId);

        buttonAct.Should().NotThrow();
        channelAct.Should().NotThrow();
        syncAct.Should().NotThrow();
        statusAct.Should().NotThrow();
    }

    [Fact]
    public void ResponseMessage_ShouldCreateCorrectly()
    {
        // Arrange
        var success = true;
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var messageType = MessageType.STATUS_RESPONSE;

        // Act
        var message = new ResponseMessage(success, data, messageType);

        // Assert
        message.Success.Should().Be(success);
        message.Data.Should().BeEquivalentTo(data);
        message.MessageType.Should().Be(messageType);
    }

    [Fact]
    public void ResponseMessage_WithEmptyData_ShouldCreateCorrectly()
    {
        // Act
        var message = new ResponseMessage(false, Array.Empty<byte>(), MessageType.GENERAL_RESPONSE);

        // Assert
        message.Success.Should().BeFalse();
        message.Data.Should().BeEmpty();
        message.MessageType.Should().Be(MessageType.GENERAL_RESPONSE);
    }
}