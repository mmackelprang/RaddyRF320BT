using FluentAssertions;
using RadioProtocol.Core;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Protocol;
using RadioProtocol.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace RadioProtocol.Tests.EndToEnd;

/// <summary>
/// End-to-end tests covering complete protocol workflows
/// </summary>
public class EndToEndTests
{
    private readonly ITestOutputHelper _output;

    public EndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Requires full protocol parsing and response message logging")]
    public async Task CompleteProtocolWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        // Queue expected responses for the workflow
        bluetoothConnection.QueueResponse("AB0220B1"); // Sync response
        bluetoothConnection.QueueResponse("AB0320C2"); // Status response  
        bluetoothConnection.QueueResponse("AB0720D6"); // General response for button press

        var deviceAddress = "00:11:22:33:44:55";

        // Act & Assert - Step 1: Connect
        _output.WriteLine("Step 1: Connecting to device...");
        var connectResult = await radioManager.ConnectAsync(deviceAddress);
        connectResult.Should().BeTrue();
        bluetoothConnection.IsConnected.Should().BeTrue();

        // Act & Assert - Step 2: Send sync request
        _output.WriteLine("Step 2: Sending sync request...");
        var syncResult = await radioManager.SendSyncRequestAsync();
        syncResult.Should().BeTrue();

        // Verify sync request was sent correctly (should have handshake from connect + sync request)
        bluetoothConnection.SentCommands.Should().HaveCount(2);
        var syncCommand = bluetoothConnection.SentCommands[1]; // Second command is the sync request
        syncCommand[0].Should().Be(ProtocolConstants.ProtocolStartByte);
        syncCommand[2].Should().Be((byte)MessageType.SyncRequest); // Command type is at index 2

        // Act & Assert - Step 3: Send status request  
        _output.WriteLine("Step 3: Sending status request...");
        var statusResult = await radioManager.SendStatusRequestAsync();
        statusResult.Should().BeTrue();

        // Act & Assert - Step 4: Send button press
        _output.WriteLine("Step 4: Sending button press...");
        var buttonResult = await radioManager.SendButtonPressAsync(ButtonType.Ptt);
        buttonResult.Should().BeTrue();

        // Act & Assert - Step 5: Send channel command
        _output.WriteLine("Step 5: Sending channel command...");
        var channelResult = await radioManager.SendChannelCommandAsync(5);
        channelResult.Should().BeTrue();

        // Verify all commands were sent (handshake + sync + status + button + channel = 5)
        bluetoothConnection.SentCommands.Should().HaveCount(5);

        // Act & Assert - Step 6: Disconnect
        _output.WriteLine("Step 6: Disconnecting...");
        await radioManager.DisconnectAsync();
        bluetoothConnection.IsConnected.Should().BeFalse();

        // Verify logging captured all activities
        logger.LogEntries.Should().NotBeEmpty();
        logger.MessagesSent.Should().HaveCount(5); // handshake, sync, status, button, channel
        logger.MessagesReceived.Should().HaveCount(3); // sync response, status response, button response

        _output.WriteLine($"Test completed successfully. Sent {bluetoothConnection.SentCommands.Count} commands.");
    }

    [Fact(Skip = "Requires error logging implementation")]
    public async Task ErrorHandling_ShouldBehaveCorrectly()
    {
        // Arrange
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        // Act & Assert - Try to send command without connection
        _output.WriteLine("Testing error handling for disconnected state...");
        var result = await radioManager.SendButtonPressAsync(ButtonType.Power);
        result.Should().BeFalse();

        // Connect and then simulate error
        await radioManager.ConnectAsync("00:11:22:33:44:55");
        bluetoothConnection.SimulateConnectionError("Test connection error");

        // Verify error state
        bluetoothConnection.IsConnected.Should().BeFalse();
        bluetoothConnection.ConnectionStatus.State.Should().Be(ConnectionState.Error);
        bluetoothConnection.ConnectionStatus.ErrorMessage.Should().Be("Test connection error");

        // Verify error was logged
        logger.LogEntries.Should().Contain(entry => entry.Contains("ERROR"));
    }

    [Fact(Skip = "Requires complete message parsing with validation")]
    public async Task MessageParsing_ShouldHandleVariousFormats()
    {
        // Arrange  
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        var receivedMessages = new List<ResponsePacket>();
        radioManager.MessageReceived += (_, responsePacket) => receivedMessages.Add(responsePacket);

        await radioManager.ConnectAsync("00:11:22:33:44:55");

        // Test various message formats from documentation
        var testMessages = new[]
        {
            "AB0220B1",     // Sync response
            "AB0320C2",     // Status response  
            "AB0720D6",     // General response
            "AB04200105CA", // Response with payload
            "AB05200210CB"  // Different response with payload
        };

        // Act
        _output.WriteLine("Testing message parsing...");
        foreach (var hexMessage in testMessages)
        {
            bluetoothConnection.QueueResponse(hexMessage);
            await radioManager.SendSyncRequestAsync(); // Trigger response processing
            await Task.Delay(10); // Allow processing
        }

        // Assert
        receivedMessages.Should().HaveCount(testMessages.Length);
        
        foreach (var message in receivedMessages)
        {
            message.RawData[0].Should().Be(ProtocolConstants.ProtocolStartByte);
            // ProtocolUtils.ValidateChecksum(message.RawData).Should().BeTrue();
            message.IsValid.Should().BeTrue();
        }

        _output.WriteLine($"Successfully parsed {receivedMessages.Count} messages.");
    }

    [Fact(Skip = "Performance test requires full implementation")]
    public async Task PerformanceTest_ShouldHandleMultipleRapidCommands()
    {
        // Arrange
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        await radioManager.ConnectAsync("00:11:22:33:44:55");

        // Queue responses for all commands
        for (int i = 0; i < 100; i++)
        {
            bluetoothConnection.QueueResponse("AB0720D6");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Send 100 rapid commands
        _output.WriteLine("Performance test: Sending 100 rapid commands...");
        var tasks = new List<Task<bool>>();
        
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(radioManager.SendButtonPressAsync(ButtonType.Ptt));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllBeEquivalentTo(true);
        bluetoothConnection.SentCommands.Should().HaveCount(100);
        
        _output.WriteLine($"Performance test completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per command: {stopwatch.ElapsedMilliseconds / 100.0:F2}ms");
        
        // Reasonable performance expectations
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact(Skip = "Concurrency test requires full implementation")]
    public async Task ConcurrencyTest_ShouldHandleSimultaneousOperations()
    {
        // Arrange
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        await radioManager.ConnectAsync("00:11:22:33:44:55");

        // Queue enough responses
        for (int i = 0; i < 50; i++)
        {
            bluetoothConnection.QueueResponse("AB0720D6");
        }

        // Act - Mix different types of concurrent operations
        _output.WriteLine("Concurrency test: Mixed operations...");
        var buttonTasks = Enumerable.Range(0, 20)
            .Select(_ => radioManager.SendButtonPressAsync(ButtonType.Ptt));
        
        var channelTasks = Enumerable.Range(1, 15)
            .Select(channel => radioManager.SendChannelCommandAsync(channel));
        
        var syncTasks = Enumerable.Range(0, 10)
            .Select(_ => radioManager.SendSyncRequestAsync());
        
        var statusTasks = Enumerable.Range(0, 5)
            .Select(_ => radioManager.SendStatusRequestAsync());

        var allTasks = buttonTasks.Concat(channelTasks).Concat(syncTasks).Concat(statusTasks);
        var results = await Task.WhenAll(allTasks);

        // Assert
        results.Should().AllBeEquivalentTo(true);
        bluetoothConnection.SentCommands.Should().HaveCount(50);
        
        // Verify no corruption in logging
        logger.MessagesSent.Should().HaveCount(50);
        logger.LogEntries.Should().NotBeEmpty();

        _output.WriteLine("Concurrency test completed successfully.");
    }

    [Fact(Skip = "Requires proper Dispose implementation in RadioManager")]
    public void ResourceCleanup_ShouldDisposeCorrectly()
    {
        // Arrange
        MockBluetoothConnection? bluetoothConnection = null;
        MockRadioLogger? logger = null;
        RadioManager? radioManager = null;

        // Act & Assert
        _output.WriteLine("Testing resource cleanup...");
        
        // Create and dispose in using blocks
        using (bluetoothConnection = new MockBluetoothConnection())
        using (logger = new MockRadioLogger())
        using (radioManager = new RadioManager(bluetoothConnection, logger))
        {
            radioManager.ConnectAsync("00:11:22:33:44:55").Wait();
            bluetoothConnection.IsConnected.Should().BeTrue();
        }

        // Resources should be properly disposed
        // Note: Mock implementations track disposal state
        bluetoothConnection.IsConnected.Should().BeFalse();
        
        _output.WriteLine("Resource cleanup test completed.");
    }
}