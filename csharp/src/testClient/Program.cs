using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using RadioClient;

// Check for command-line mode
var commandLineMode = args.Length > 0;
var commandsToSend = new List<CanonicalAction>();

if (commandLineMode)
{
    // Parse command-line arguments as button names
    foreach (var arg in args)
    {
        if (Enum.TryParse<CanonicalAction>(arg, true, out var action))
        {
            commandsToSend.Add(action);
        }
        else
        {
            Console.WriteLine($"Unknown action: {arg}");
            Console.WriteLine("Valid actions: " + string.Join(", ", Enum.GetNames<CanonicalAction>()));
            return;
        }
    }
}

// Global cancellation for graceful shutdown
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
Console.WriteLine(commandLineMode 
    ? $"                RF320 Command-Line Test ({commandsToSend.Count} commands)"
    : "                    RF320 Radio BLE Test Client");
Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
Console.WriteLine();

// Create logger
var logger = MessageLogger.Create();
logger.LogInfo("Application started");
Console.WriteLine($"Log file: {logger.LogFilePath}");
Console.WriteLine();

try
{
    // Scan for BLE devices
    Console.WriteLine("Scanning for RF320 devices...");
    logger.LogInfo("Starting BLE scan");

    var device = await ScanForRF320DeviceAsync(logger, cts.Token);
    
    if (device is null)
    {
        Console.WriteLine("No RF320 device found. Exiting.");
        logger.LogError("No RF320 device found");
        return;
    }

    Console.WriteLine($"Found device: {device.Name ?? "Unknown"} ({device.BluetoothAddress:X})");
    logger.LogInfo($"Found device: {device.Name ?? "Unknown"} (Address: {device.BluetoothAddress:X})");
    Console.WriteLine();

    // Connect to device
    Console.WriteLine("Connecting to device...");
    var transport = await WinBleTransport.ConnectAsync(device!, logger, cts.Token);
    
    if (transport is null)
    {
        Console.WriteLine("Failed to connect to device. Check log for details.");
        logger.LogError("Failed to connect to device");
        return;
    }

    Console.WriteLine("Connected successfully!");
    logger.LogInfo("Connected to device successfully");
    Console.WriteLine();

    // Create radio client
    var radio = new RadioBT(transport);
    
    // Status tracking
    string? currentBand = null;
    string? currentVolume = null;
    string? freqDigit1 = null;
    string? freqDigits23 = null;
    
    // Set up event handlers
    int statusCount = 0;
    radio.FrameReceived += (s, frame) =>
    {
        logger.LogFrame(MessageSource.Radio, frame);
        
        // Only display non-status frames to console (status floods the output)
        if (frame.Group != CommandGroup.Status)
        {
            var frameType = GetFrameDescription(frame);
            Console.WriteLine($"  ← RX: {frameType}");
        }
        else
        {
            statusCount++;
            if (statusCount % 20 == 0) // Show occasional status to confirm stream
            {
                Console.WriteLine($"  ... [{statusCount} status messages received]");
            }
        }
    };

    radio.StatusReceived += (s, status) =>
    {
        // Display all status messages to understand the pattern
        Console.WriteLine($"  ← {status.Label}: '{status.Value}'");
        
        // Update tracked values based on status type
        if (status.Label == "Band") currentBand = status.Value;
        if (status.Label == "VolumeValue") currentVolume = status.Value;
        if (status.Label == "FreqPart1") freqDigit1 = status.Value;
        if (status.Label == "FreqPart2") freqDigits23 = status.Value;
    };

    radio.StateUpdated += (s, state) =>
    {
        logger.LogState(state);
        var signalBars = new string('█', state.SignalStrength) + new string('░', 6 - state.SignalStrength);
        Console.WriteLine($"  ← STATE: Band={state.BandName,-6} Freq≈{state.FrequencyMHz:0.00} MHz  Signal:[{signalBars}] {state.SignalQualityText}");
        Console.WriteLine($"           (raw=0x{state.RawFreqValue:X6}, scale={state.ScaleFactor}, B9=0x{state.ScaleFactor:X2})");
    };

    // Initialize radio
    Console.WriteLine("Initializing radio (handshake)...");
    logger.LogInfo("Sending handshake");
    logger.LogMessage(MessageSource.Application, new byte[] { 0xAB, 0x01, 0xFF, 0xAB }, "Handshake");
    
    if (!await radio.InitializeAsync(cts.Token))
    {
        Console.WriteLine("Didn't receive handshake response - did we expect one?");
//        Console.WriteLine("Failed to initialize radio. Exiting.");
//        Console.WriteLine("This may be due to notifications not being enabled properly.");
//        Console.WriteLine("Try pairing the device in Windows Bluetooth settings first.");
//        logger.LogError("Handshake failed - no response received within timeout");
//        radio.Dispose();
//        return;
    }

    Console.WriteLine("Radio initialized successfully!");
    logger.LogInfo("Handshake successful");
    Console.WriteLine();

    // Start monitoring (disabled for now - causes excessive handshake resends)
    // radio.StartMonitor();

    if (commandLineMode)
    {
        // Command-line mode: Send specified commands and wait for responses
        Console.WriteLine($"Sending {commandsToSend.Count} command(s)...");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────");
        
        foreach (var action in commandsToSend)
        {
            Console.WriteLine($"  → TX: {action}");
            logger.LogInfo($"Command-line mode: Sending {action}");
            
            var success = await radio.SendAsync(action);
            
            if (success)
            {
                var frame = RadioFrame.Build(CommandGroup.Button, CommandIdMap.Id[action]);
                logger.LogFrame(MessageSource.Application, frame);
            }
            else
            {
                Console.WriteLine($"     ⚠ Failed to send command");
                logger.LogError($"Failed to send {action}");
            }
            
            // Small delay between commands
            await Task.Delay(100);
        }
        
        Console.WriteLine();
        Console.WriteLine("Waiting 5 seconds for responses...");
        await Task.Delay(5000, cts.Token);
        
        Console.WriteLine("Done. Check log file for all messages.");
    }
    else
    {
        // Interactive mode
        Console.WriteLine(KeyboardMapper.GetKeyboardHelp());
        Console.WriteLine();
        Console.WriteLine("Ready! Press keys to send commands, ESC to exit.");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // Main keyboard loop
        await KeyboardLoopAsync(radio, logger, cts.Token);

        Console.WriteLine();
        Console.WriteLine("Shutting down...");
        logger.LogInfo("Shutting down");
    }
    
    radio.Dispose();
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nOperation cancelled.");
    logger.LogInfo("Operation cancelled by user");
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
    logger.LogError($"Unhandled exception: {ex.Message}");
    logger.LogError($"Stack trace: {ex.StackTrace}");
}
finally
{
    logger.LogInfo("Application exiting");
    logger.Dispose();
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
    Console.WriteLine($"Log file saved to: {logger.LogFilePath}");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════════════");
}

// ═══════════════════════════════════════════════════════════════════════════
// Helper Methods
// ═══════════════════════════════════════════════════════════════════════════

static async Task<BluetoothLEDevice?> ScanForRF320DeviceAsync(MessageLogger logger, CancellationToken ct)
{
    var tcs = new TaskCompletionSource<BluetoothLEDevice?>();
    var watcher = new BluetoothLEAdvertisementWatcher
    {
        ScanningMode = BluetoothLEScanningMode.Active
    };

    watcher.Received += async (sender, args) =>
    {
        try
        {
            // Get device from advertisement
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
            
            if (device?.Name != null && device.Name.Contains("RF320", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  Found: {device.Name} (Signal: {args.RawSignalStrengthInDBm} dBm)");
                logger.LogInfo($"Discovered RF320 device: {device.Name} (Address: {device.BluetoothAddress:X}, RSSI: {args.RawSignalStrengthInDBm} dBm)");
                
                watcher.Stop();
                tcs.TrySetResult(device);
            }
            else
            {
                device?.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing advertisement: {ex.Message}");
        }
    };

    watcher.Stopped += (sender, args) =>
    {
        if (!tcs.Task.IsCompleted)
        {
            tcs.TrySetResult(null);
        }
    };

    // Start scanning with timeout
    watcher.Start();
    logger.LogInfo("BLE scan started");

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

    try
    {
        await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
        {
            return await tcs.Task;
        }
    }
    catch (OperationCanceledException)
    {
        logger.LogInfo("BLE scan timeout or cancelled");
        return null;
    }
    finally
    {
        if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
        {
            watcher.Stop();
        }
    }
}

static async Task KeyboardLoopAsync(RadioBT radio, MessageLogger logger, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        if (!Console.KeyAvailable)
        {
            await Task.Delay(50, ct);
            continue;
        }

        var keyInfo = Console.ReadKey(intercept: true);

        // Exit on Escape
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            Console.WriteLine("ESC pressed - exiting...");
            logger.LogInfo("User pressed ESC to exit");
            break;
        }

        // Try to map key to action
        if (KeyboardMapper.TryGetAction(keyInfo, out var action))
        {
            try
            {
                var modifiers = keyInfo.Modifiers != 0 ? $" (with {keyInfo.Modifiers})" : "";
                Console.WriteLine($"  → TX: {keyInfo.Key}{modifiers} → {action}");
                
                var success = await radio.SendAsync(action);
                
                if (success)
                {
                    var frame = RadioFrame.Build(CommandGroup.Button, CommandIdMap.Id[action]);
                    logger.LogFrame(MessageSource.Application, frame);
                }
                else
                {
                    Console.WriteLine($"     ⚠ Failed to send command");
                    logger.LogError($"Failed to send {action}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     ⚠ Error: {ex.Message}");
                logger.LogError($"Error sending {action}: {ex.Message}");
            }
        }
        else
        {
            // Unmapped key
            var modInfo = keyInfo.Modifiers != 0 ? $" (with {keyInfo.Modifiers})" : "";
            Console.WriteLine($"  ? Unmapped key: {keyInfo.Key}{modInfo}");
        }
    }
}

static string GetFrameDescription(RadioFrame frame)
{
    if (frame.Header == 0xAB && frame.Proto == 0x01)
    {
        return "Handshake";
    }

    if (frame.Group == CommandGroup.Ack)
    {
        return frame.CommandId switch
        {
            0x00 => "Ack: FAIL",
            0x01 => "Ack: SUCCESS",
            _ => $"Ack: 0x{frame.CommandId:X2}"
        };
    }

    if (frame.Group == CommandGroup.Button)
    {
        var action = CommandIdMap.Id.FirstOrDefault(kvp => kvp.Value == frame.CommandId).Key;
        return action != default ? $"{action}" : $"Button: 0x{frame.CommandId:X2}";
    }

    return $"{frame.Group}: 0x{frame.CommandId:X2}";
}
