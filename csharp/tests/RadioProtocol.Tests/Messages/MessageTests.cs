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
        ProtocolConstants.ProtocolStartByte.Should().Be(0xAB);
        ProtocolConstants.MessageVersion.Should().Be(0x20);
        ProtocolConstants.DefaultRadioId.Should().Be(0x01);
        ProtocolConstants.MaxMessageLength.Should().Be(255);
        ProtocolConstants.MinMessageLength.Should().Be(4);
    }

    [Fact]
    public void ButtonType_ShouldHaveExpectedValues()
    {
        // Assert - Check some key button types
        ButtonType.Power.Should().Be(ButtonType.Power);
        ButtonType.VolumeUp.Should().Be(ButtonType.VolumeUp);
        ButtonType.VolumeDown.Should().Be(ButtonType.VolumeDown);
        ButtonType.ChannelUp.Should().Be(ButtonType.ChannelUp);
        ButtonType.ChannelDown.Should().Be(ButtonType.ChannelDown);
        ButtonType.Ptt.Should().Be(ButtonType.Ptt);
        ButtonType.Menu.Should().Be(ButtonType.Menu);
        ButtonType.Select.Should().Be(ButtonType.Select);
        ButtonType.Back.Should().Be(ButtonType.Back);
        ButtonType.Scan.Should().Be(ButtonType.Scan);
    }

    [Fact]
    public void MessageType_ShouldHaveExpectedValues()
    {
        // Assert
        MessageType.ButtonPress.Should().Be(MessageType.ButtonPress);
        MessageType.ChannelCommand.Should().Be(MessageType.ChannelCommand);
        MessageType.SyncRequest.Should().Be(MessageType.SyncRequest);
        MessageType.SyncResponse.Should().Be(MessageType.SyncResponse);
        MessageType.StatusRequest.Should().Be(MessageType.StatusRequest);
        MessageType.StatusResponse.Should().Be(MessageType.StatusResponse);
        MessageType.GeneralResponse.Should().Be(MessageType.GeneralResponse);
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
    [InlineData(ButtonType.Power, "Power")]
    [InlineData(ButtonType.VolumeUp, "VolumeUp")]
    [InlineData(ButtonType.Ptt, "Ptt")]
    [InlineData(ButtonType.Menu, "Menu")]
    public void ButtonType_ToString_ShouldReturnCorrectName(ButtonType buttonType, string expectedName)
    {
        // Act
        var result = buttonType.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(MessageType.ButtonPress, "ButtonPress")]
    [InlineData(MessageType.SyncRequest, "SyncRequest")]
    [InlineData(MessageType.StatusResponse, "StatusResponse")]
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
        var buttonType = ButtonType.Ptt;
        var radioId = 0x02;

        // Act
        var message = new ButtonPressMessage(buttonType, radioId);

        // Assert
        message.ButtonType.Should().Be(buttonType);
        message.RadioId.Should().Be((byte)radioId);
        message.MessageType.Should().Be(MessageType.ButtonPress);
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
        message.MessageType.Should().Be(MessageType.ChannelCommand);
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
        message.MessageType.Should().Be(MessageType.SyncRequest);
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
        message.MessageType.Should().Be(MessageType.StatusRequest);
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
        var buttonAct = () => new ButtonPressMessage(ButtonType.Power, radioId);
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
        var messageType = MessageType.StatusResponse;

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
        var message = new ResponseMessage(false, Array.Empty<byte>(), MessageType.GeneralResponse);

        // Assert
        message.Success.Should().BeFalse();
        message.Data.Should().BeEmpty();
        message.MessageType.Should().Be(MessageType.GeneralResponse);
    }
}