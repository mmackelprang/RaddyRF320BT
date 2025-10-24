using FluentAssertions;
using RadioProtocol.Core.Logging;
using RadioProtocol.Tests.Mocks;
using Xunit;

namespace RadioProtocol.Tests.Logging;

/// <summary>
/// Tests for the logging system
/// </summary>
public class RadioLoggerTests
{
    [Fact]
    public void MockRadioLogger_ShouldCaptureAllLogTypes()
    {
        // Arrange
        var logger = new MockRadioLogger();
        var testData = new byte[] { 0xAB, 0x01, 0xFF };
        var testMessage = "Test message";
        var testException = new InvalidOperationException("Test exception");

        // Act
        logger.LogRawDataSent(testData);
        logger.LogRawDataReceived(testData);
        logger.LogMessageSent("TestType", new { Data = "test" });
        logger.LogMessageReceived("TestType", new { Data = "test" });
        logger.LogInfo(testMessage);
        logger.LogWarning(testMessage);
        logger.LogError(testException, testMessage);
        logger.LogDebug(testMessage);

        // Assert
        logger.LogEntries.Should().HaveCount(8);
        logger.RawDataSent.Should().HaveCount(1);
        logger.RawDataReceived.Should().HaveCount(1);
        logger.MessagesSent.Should().HaveCount(1);
        logger.MessagesReceived.Should().HaveCount(1);
        
        logger.LogEntries.Should().Contain(e => e.Contains("RAW SENT"));
        logger.LogEntries.Should().Contain(e => e.Contains("RAW RECEIVED"));
        logger.LogEntries.Should().Contain(e => e.Contains("MESSAGE SENT"));
        logger.LogEntries.Should().Contain(e => e.Contains("MESSAGE RECEIVED"));
        logger.LogEntries.Should().Contain(e => e.Contains("INFO"));
        logger.LogEntries.Should().Contain(e => e.Contains("WARNING"));
        logger.LogEntries.Should().Contain(e => e.Contains("ERROR"));
        logger.LogEntries.Should().Contain(e => e.Contains("DEBUG"));
    }

    [Fact]
    public void MockRadioLogger_Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        var logger = new MockRadioLogger();
        logger.LogInfo("Test message");
        logger.LogRawDataSent(new byte[] { 0x01 });

        // Act
        logger.Clear();

        // Assert
        logger.LogEntries.Should().BeEmpty();
        logger.RawDataSent.Should().BeEmpty();
        logger.RawDataReceived.Should().BeEmpty();
        logger.MessagesSent.Should().BeEmpty();
        logger.MessagesReceived.Should().BeEmpty();
    }

    [Fact]
    public void MockRadioLogger_ShouldIncludeContextInformation()
    {
        // Arrange
        var logger = new MockRadioLogger();
        
        // Act
        logger.LogInfo("Test message", "TestMethod", "TestClass.cs");

        // Assert
        logger.LogEntries.Should().HaveCount(1);
        logger.LogEntries[0].Should().Contain("TestClass.TestMethod");
        logger.LogEntries[0].Should().Contain("Test message");
    }

    [Fact]
    public void FileLoggerProvider_ShouldCreateFileLogger()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            // Act
            using var provider = new FileLoggerProvider(tempFile);
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            logger.Should().NotBeNull();
            logger.Should().BeOfType<FileLogger>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}

/// <summary>
/// Tests for Bluetooth mock implementation
/// </summary>
public class MockBluetoothConnectionTests
{
    [Fact]
    public void MockBluetoothConnection_InitialState_ShouldBeDisconnected()
    {
        // Arrange & Act
        using var connection = new MockBluetoothConnection();

        // Assert
        connection.IsConnected.Should().BeFalse();
        connection.ConnectionStatus.State.Should().Be(RadioProtocol.Core.Constants.ConnectionState.Disconnected);
        connection.SentCommands.Should().BeEmpty();
        connection.ResponseQueueCount.Should().Be(0);
    }

    [Fact]
    public async Task ConnectAsync_ShouldUpdateConnectionState()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        var stateChangedEventFired = false;
        connection.ConnectionStateChanged += (_, _) => stateChangedEventFired = true;

        // Act
        var result = await connection.ConnectAsync("00:11:22:33:44:55");

        // Assert
        result.Should().BeTrue();
        connection.IsConnected.Should().BeTrue();
        connection.ConnectionStatus.State.Should().Be(RadioProtocol.Core.Constants.ConnectionState.Connected);
        stateChangedEventFired.Should().BeTrue();
    }

    [Fact]
    public async Task SendDataAsync_WhenConnected_ShouldStoreSentData()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        await connection.ConnectAsync("00:11:22:33:44:55");
        var testData = new byte[] { 0xAB, 0x01, 0xFF };

        // Act
        var result = await connection.SendDataAsync(testData);

        // Assert
        result.Should().BeTrue();
        connection.SentCommands.Should().HaveCount(1);
        connection.SentCommands[0].Should().BeEquivalentTo(testData);
    }

    [Fact]
    public async Task SendDataAsync_WhenDisconnected_ShouldFail()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        var testData = new byte[] { 0xAB, 0x01, 0xFF };

        // Act
        var result = await connection.SendDataAsync(testData);

        // Assert
        result.Should().BeFalse();
        connection.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task QueueResponse_ShouldTriggerDataReceivedEvent()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        await connection.ConnectAsync("00:11:22:33:44:55");
        
        byte[]? receivedData = null;
        connection.DataReceived += (_, data) => receivedData = data;
        
        var responseData = new byte[] { 0xAB, 0x02, 0x20 };
        connection.QueueResponse(responseData);

        // Act
        await connection.SendDataAsync(new byte[] { 0x01 }); // Trigger response processing

        // Assert
        await Task.Delay(10); // Allow event to fire
        receivedData.Should().BeEquivalentTo(responseData);
    }

    [Fact]
    public void QueueResponse_WithHexString_ShouldConvertCorrectly()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        var hexData = "AB0220";

        // Act
        connection.QueueResponse(hexData);

        // Assert
        connection.ResponseQueueCount.Should().Be(1);
    }

    [Fact]
    public void SimulateConnectionError_ShouldUpdateStateToError()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        var errorMessage = "Test connection error";
        var stateChanged = false;
        connection.ConnectionStateChanged += (_, info) => 
        {
            stateChanged = true;
            info.State.Should().Be(RadioProtocol.Core.Constants.ConnectionState.Error);
            info.ErrorMessage.Should().Be(errorMessage);
        };

        // Act
        connection.SimulateConnectionError(errorMessage);

        // Assert
        connection.IsConnected.Should().BeFalse();
        connection.ConnectionStatus.State.Should().Be(RadioProtocol.Core.Constants.ConnectionState.Error);
        connection.ConnectionStatus.ErrorMessage.Should().Be(errorMessage);
        stateChanged.Should().BeTrue();
    }

    [Fact]
    public void ClearSentCommands_ShouldRemoveAllSentData()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        connection.ConnectAsync("00:11:22:33:44:55").Wait();
        connection.SendDataAsync(new byte[] { 0x01 }).Wait();
        connection.SendDataAsync(new byte[] { 0x02 }).Wait();

        // Act
        connection.ClearSentCommands();

        // Assert
        connection.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public void ClearResponseQueue_ShouldRemoveAllQueuedResponses()
    {
        // Arrange
        using var connection = new MockBluetoothConnection();
        connection.QueueResponse(new byte[] { 0x01 });
        connection.QueueResponse(new byte[] { 0x02 });

        // Act
        connection.ClearResponseQueue();

        // Assert
        connection.ResponseQueueCount.Should().Be(0);
    }
}