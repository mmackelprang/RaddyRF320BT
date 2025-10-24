using FluentAssertions;
using RadioProtocol.Core.Constants;
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

    [Fact]
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

        // Verify sync request was sent correctly
        bluetoothConnection.SentCommands.Should().HaveCount(1);
        var syncCommand = bluetoothConnection.SentCommands[0];
        syncCommand[0].Should().Be(ProtocolConstants.PROTOCOL_START_BYTE);
        syncCommand[1].Should().Be((byte)MessageType.SYNC_REQUEST);

        // Act & Assert - Step 3: Send status request  
        _output.WriteLine("Step 3: Sending status request...");
        var statusResult = await radioManager.SendStatusRequestAsync();
        statusResult.Should().BeTrue();

        // Act & Assert - Step 4: Send button press
        _output.WriteLine("Step 4: Sending button press...");
        var buttonResult = await radioManager.SendButtonPressAsync(ButtonType.PTT);
        buttonResult.Should().BeTrue();

        // Act & Assert - Step 5: Send channel command
        _output.WriteLine("Step 5: Sending channel command...");
        var channelResult = await radioManager.SendChannelCommandAsync(5);
        channelResult.Should().BeTrue();

        // Verify all commands were sent
        bluetoothConnection.SentCommands.Should().HaveCount(4);

        // Act & Assert - Step 6: Disconnect
        _output.WriteLine("Step 6: Disconnecting...");
        await radioManager.DisconnectAsync();
        bluetoothConnection.IsConnected.Should().BeFalse();

        // Verify logging captured all activities
        logger.LogEntries.Should().NotBeEmpty();
        logger.MessagesSent.Should().HaveCount(4);
        logger.MessagesReceived.Should().HaveCount(3);

        _output.WriteLine($"Test completed successfully. Sent {bluetoothConnection.SentCommands.Count} commands.");
    }

    [Fact]
    public async Task ErrorHandling_ShouldBehaveCorrectly()
    {
        // Arrange
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        // Act & Assert - Try to send command without connection
        _output.WriteLine("Testing error handling for disconnected state...");
        var result = await radioManager.SendButtonPressAsync(ButtonType.POWER);
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

    [Fact]
    public async Task MessageParsing_ShouldHandleVariousFormats()
    {
        // Arrange  
        using var bluetoothConnection = new MockBluetoothConnection();
        var logger = new MockRadioLogger();
        var radioManager = new RadioManager(bluetoothConnection, logger);

        var receivedMessages = new List<byte[]>();
        radioManager.MessageReceived += (_, data) => receivedMessages.Add(data);

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
            message[0].Should().Be(ProtocolConstants.PROTOCOL_START_BYTE);
            ProtocolUtils.ValidateChecksum(message).Should().BeTrue();
        }

        _output.WriteLine($"Successfully parsed {receivedMessages.Count} messages.");
    }

    [Fact]
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
            tasks.Add(radioManager.SendButtonPressAsync(ButtonType.PTT));
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

    [Fact]
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
            .Select(_ => radioManager.SendButtonPressAsync(ButtonType.PTT));
        
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

    [Fact]
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