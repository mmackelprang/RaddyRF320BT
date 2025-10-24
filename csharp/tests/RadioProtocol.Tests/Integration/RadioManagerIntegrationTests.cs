using FluentAssertions;
using RadioProtocol.Core;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Integration;

/// <summary>
/// Integration tests for the complete radio protocol system
/// </summary>
public class RadioManagerIntegrationTests : IDisposable
{
    private readonly MockRadioLogger _logger;
    private readonly MockBluetoothConnection _mockBluetooth;
    private readonly RadioManager _radioManager;

    public RadioManagerIntegrationTests()
    {
        _logger = new MockRadioLogger();
        _mockBluetooth = new MockBluetoothConnection();
        
        // Create RadioManager with mock dependencies
        _radioManager = new RadioManager(_logger);
        
        // Replace the internal Bluetooth connection with our mock
        // Note: In a real implementation, we'd need dependency injection
        // For now, we'll test the public interface
    }

    [Fact]
    public async Task ConnectAsync_ShouldEstablishConnection()
    {
        // Arrange
        var deviceAddress = "00:11:22:33:44:55";
        var connectionStateChanged = false;
        
        _radioManager.ConnectionStateChanged += (_, _) => connectionStateChanged = true;

        // Act
        var result = await _radioManager.ConnectAsync(deviceAddress);

        // Assert
        result.Should().BeTrue();
        connectionStateChanged.Should().BeTrue();
    }

    [Fact]
    public async Task PressButtonAsync_ShouldSendCorrectCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.PressButtonAsync(ButtonType.Number1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData![3].Should().Be((byte)ButtonType.Number1);
    }

    [Fact]
    public async Task PressNumberAsync_ShouldSendCorrectNumberCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.PressNumberAsync(5);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData![3].Should().Be((byte)ButtonType.Number5);
    }

    [Fact]
    public async Task PressNumberAsync_WithLongPress_ShouldSendLongPressCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.PressNumberAsync(5, longPress: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData![3].Should().Be((byte)ButtonType.Number5Long);
    }

    [Fact]
    public async Task AdjustVolumeAsync_Up_ShouldSendVolumeUpCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.AdjustVolumeAsync(up: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData![3].Should().Be((byte)ButtonType.VolumeUp);
    }

    [Fact]
    public async Task AdjustVolumeAsync_Down_ShouldSendVolumeDownCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.AdjustVolumeAsync(up: false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData![3].Should().Be((byte)ButtonType.VolumeDown);
    }

    [Fact]
    public async Task NavigateAsync_ShouldSendCorrectNavigationCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Test all navigation combinations
        var upResult = await _radioManager.NavigateAsync(up: true, longPress: false);
        var downResult = await _radioManager.NavigateAsync(up: false, longPress: false);
        var upLongResult = await _radioManager.NavigateAsync(up: true, longPress: true);
        var downLongResult = await _radioManager.NavigateAsync(up: false, longPress: true);

        // Assert
        upResult.SentData![3].Should().Be((byte)ButtonType.UpShort);
        downResult.SentData![3].Should().Be((byte)ButtonType.DownShort);
        upLongResult.SentData![3].Should().Be((byte)ButtonType.UpLong);
        downLongResult.SentData![3].Should().Be((byte)ButtonType.DownLong);
    }

    [Fact]
    public async Task SendHandshakeAsync_ShouldSendHandshakeCommand()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act
        var result = await _radioManager.SendHandshakeAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SentData.Should().NotBeNull();
        result.SentData.Should().HaveCount(4);
        result.SentData![0].Should().Be(ProtocolConstants.ProtocolStartByte);
        result.SentData![1].Should().Be(ProtocolConstants.MessageLengthHandshake);
        result.SentData![2].Should().Be(ProtocolConstants.DataHandshake);
        result.SentData![3].Should().Be(ProtocolConstants.ProtocolStartByte);
    }

    [Fact]
    public async Task SendCommandAsync_WhenNotConnected_ShouldFail()
    {
        // Act
        var result = await _radioManager.SendCommandAsync(new byte[] { 0x01, 0x02, 0x03 });

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Not connected to radio device");
    }

    public void Dispose()
    {
        _radioManager?.Dispose();
        _mockBluetooth?.Dispose();
    }
}

/// <summary>
/// End-to-end system tests based on documented command-response sequences
/// </summary>
public class SystemEndToEndTests : IDisposable
{
    private readonly MockRadioLogger _logger;
    private readonly RadioManager _radioManager;

    public SystemEndToEndTests()
    {
        _logger = new MockRadioLogger();
        _radioManager = new RadioManager(_logger);
    }

    [Fact]
    public async Task CompleteHandshakeSequence_ShouldFollowDocumentedProtocol()
    {
        // Arrange - Based on COMMAND_RESPONSE_SEQUENCES.md
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Send handshake (Frame 828 from documentation)
        var handshakeResult = await _radioManager.SendHandshakeAsync();

        // Assert
        handshakeResult.Success.Should().BeTrue();
        handshakeResult.SentData.Should().BeEquivalentTo(new byte[] { 0xAB, 0x01, 0xFF, 0xAB });
        
        // Verify logging
        _logger.MessagesSent.Should().Contain(m => m.messageType == "Handshake");
    }

    [Fact]
    public async Task NumberButtonSequence_ShouldSendCorrectCommands()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Send number sequence 1, 2, 3
        var results = new List<CommandResult>();
        for (int i = 1; i <= 3; i++)
        {
            results.Add(await _radioManager.PressNumberAsync(i));
        }

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results[0].SentData![3].Should().Be(0x01); // Number 1
        results[1].SentData![3].Should().Be(0x02); // Number 2
        results[2].SentData![3].Should().Be(0x03); // Number 3
    }

    [Fact]
    public async Task VolumeControlSequence_ShouldWorkCorrectly()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Volume up, then down
        var upResult = await _radioManager.AdjustVolumeAsync(up: true);
        var downResult = await _radioManager.AdjustVolumeAsync(up: false);

        // Assert
        upResult.Success.Should().BeTrue();
        downResult.Success.Should().BeTrue();
        upResult.SentData![3].Should().Be(0x12); // Volume up
        downResult.SentData![3].Should().Be(0x13); // Volume down
    }

    [Fact]
    public async Task DocumentedButtonSequence_ShouldExecuteCorrectly()
    {
        // Arrange - Based on test sequence from documentation
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        var testSequence = new[]
        {
            (ButtonType.Band, "Band button"),
            (ButtonType.Number1, "Number 1"),
            (ButtonType.Number2, "Number 2"),
            (ButtonType.Number3, "Number 3"),
            (ButtonType.VolumeUp, "Volume up"),
            (ButtonType.VolumeDown, "Volume down"),
            (ButtonType.UpShort, "Navigate up"),
            (ButtonType.DownShort, "Navigate down"),
            (ButtonType.Frequency, "Frequency button"),
            (ButtonType.Memo, "Memory button"),
            (ButtonType.Record, "Record button")
        };

        // Act
        var results = new List<CommandResult>();
        foreach (var (buttonType, description) in testSequence)
        {
            var result = await _radioManager.PressButtonAsync(buttonType);
            results.Add(result);
        }

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results.Should().HaveCount(testSequence.Length);
        
        // Verify specific button data bytes
        results[0].SentData![3].Should().Be((byte)ButtonType.Band);
        results[1].SentData![3].Should().Be((byte)ButtonType.Number1);
        results[4].SentData![3].Should().Be((byte)ButtonType.VolumeUp);
        results[5].SentData![3].Should().Be((byte)ButtonType.VolumeDown);
    }

    [Fact]
    public async Task LongPressSequence_ShouldUseCorrectCommands()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Test memory channel access via long press
        var memoryChannels = new List<CommandResult>();
        for (int i = 1; i <= 5; i++)
        {
            memoryChannels.Add(await _radioManager.PressNumberAsync(i, longPress: true));
        }

        // Assert
        memoryChannels.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        memoryChannels[0].SentData![3].Should().Be(0x35); // Number 1 long press
        memoryChannels[1].SentData![3].Should().Be(0x36); // Number 2 long press
        memoryChannels[2].SentData![3].Should().Be(0x37); // Number 3 long press
        memoryChannels[3].SentData![3].Should().Be(0x38); // Number 4 long press
        memoryChannels[4].SentData![3].Should().Be(0x39); // Number 5 long press
    }

    [Fact]
    public async Task SpecialFunctionButtons_ShouldSendCorrectCommands()
    {
        // Arrange
        await _radioManager.ConnectAsync("00:11:22:33:44:55");

        // Act - Test special function buttons
        var specialButtons = new[]
        {
            ButtonType.Sos,
            ButtonType.SosLong,
            ButtonType.AlarmClick,
            ButtonType.AlarmLong,
            ButtonType.Bluetooth,
            ButtonType.Power,
            ButtonType.PowerLong
        };

        var results = new List<CommandResult>();
        foreach (var buttonType in specialButtons)
        {
            results.Add(await _radioManager.PressButtonAsync(buttonType));
        }

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results[0].SentData![3].Should().Be(0x2A); // SOS
        results[1].SentData![3].Should().Be(0x2B); // SOS long
        results[2].SentData![3].Should().Be(0x31); // Alarm click
        results[3].SentData![3].Should().Be(0x32); // Alarm long
        results[4].SentData![3].Should().Be(0x1C); // Bluetooth
        results[5].SentData![3].Should().Be(0x14); // Power
        results[6].SentData![3].Should().Be(0x45); // Power long
    }

    public void Dispose()
    {
        _radioManager?.Dispose();
    }
}