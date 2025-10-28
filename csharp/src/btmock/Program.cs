using BtMock.Config;
using BtMock.Logging;
using BtMock.Bluetooth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BtMock;

/// <summary>
/// Main entry point for the Bluetooth Mock Radio application.
/// This application simulates a Bluetooth LE radio device for protocol reverse engineering.
/// </summary>
class Program
{
    // Configuration and logging instances
    private static IConfiguration? _configuration;
    private static ILogger? _logger;
    private static MessageLogger? _messageLogger;
    private static BluetoothPeripheral? _peripheral;
    
    // Current message tag for logging
    private static string _currentMessageTag = string.Empty;
    private static readonly object _tagLock = new();

    static async Task Main(string[] args)
    {
        try
        {
            // Initialize configuration and logging
            InitializeConfiguration();
            InitializeLogging();
            
            Console.WriteLine("===========================================");
            Console.WriteLine("  Bluetooth Mock Radio Device (btmock)");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            
            // Load configuration
            var config = _configuration!.GetSection("BluetoothConfig").Get<BluetoothConfiguration>()
                ?? throw new InvalidOperationException("Failed to load Bluetooth configuration");
            
            DisplayConfiguration(config);
            
            // Initialize message logger
            var logPath = _configuration.GetValue<string>("Logging:CsvLogPath") ?? "logs/btmock_log.csv";
            _messageLogger = new MessageLogger(logPath);
            
            // Initialize Bluetooth peripheral
            _peripheral = new BluetoothPeripheral(config, _logger!);
            _peripheral.MessageReceived += OnMessageReceived;
            _peripheral.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Start advertising
            Console.WriteLine("\nStarting Bluetooth LE peripheral...");
            await _peripheral.StartAdvertisingAsync();
            
            Console.WriteLine("\n✓ Mock radio is now advertising and ready for connections!");
            DisplayInstructions();
            
            // Main console loop
            await ConsoleLoopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Fatal error: {ex.Message}");
            _logger?.LogError(ex, "Fatal error in main program");
        }
        finally
        {
            // Cleanup
            if (_peripheral != null)
            {
                await _peripheral.StopAsync();
            }
            _messageLogger?.Dispose();
        }
    }

    /// <summary>
    /// Initializes the configuration from config.json file.
    /// </summary>
    private static void InitializeConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Initializes console logging.
    /// </summary>
    private static void InitializeLogging()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        _logger = loggerFactory.CreateLogger<Program>();
        // Note: Logger factory is intentionally not disposed to keep logger functional
        // In a production app, consider using dependency injection for proper lifecycle management
    }

    /// <summary>
    /// Displays the current configuration settings.
    /// </summary>
    private static void DisplayConfiguration(BluetoothConfiguration config)
    {
        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Device Name:        {config.DeviceName}");
        Console.WriteLine($"  Device Address:     {config.DeviceAddress}");
        Console.WriteLine($"  Service UUID:       {config.ServiceUUID}");
        Console.WriteLine($"  Write Char UUID:    {config.WriteCharacteristicUUID}");
        Console.WriteLine($"  Notify Char UUID:   {config.NotifyCharacteristicUUID}");
    }

    /// <summary>
    /// Displays usage instructions to the console.
    /// </summary>
    private static void DisplayInstructions()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("INSTRUCTIONS:");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("  [t] - Set/change message tag for logging");
        Console.WriteLine("  [c] - Clear current message tag");
        Console.WriteLine("  [r] - Send canned response (handshake)");
        Console.WriteLine("  [s] - Send canned response (status)");
        Console.WriteLine("  [h] - Send custom hex string as response");
        Console.WriteLine("  [i] - Display these instructions again");
        Console.WriteLine("  [q] - Quit the application");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();
        Console.WriteLine("The mock radio will log all received messages with timestamps,");
        Console.WriteLine("raw data (hex + text), and the current tag to a CSV file.");
        Console.WriteLine();
    }

    /// <summary>
    /// Main console input loop for handling user commands.
    /// </summary>
    private static async Task ConsoleLoopAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                
                try
                {
                    switch (key)
                    {
                        case ConsoleKey.T:
                            SetMessageTag();
                            break;
                            
                        case ConsoleKey.C:
                            ClearMessageTag();
                            break;
                            
                        case ConsoleKey.R:
                            await SendCannedResponseAsync("HandshakeResponse");
                            break;
                            
                        case ConsoleKey.S:
                            await SendCannedResponseAsync("StatusResponse");
                            break;
                            
                        case ConsoleKey.H:
                            await SendCustomHexResponseAsync();
                            break;
                            
                        case ConsoleKey.I:
                            DisplayInstructions();
                            break;
                            
                        case ConsoleKey.Q:
                            Console.WriteLine("\nShutting down...");
                            cancellationTokenSource.Cancel();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n✗ Error processing command: {ex.Message}");
                    _logger?.LogError(ex, "Error processing console command");
                }
            }
            
            await Task.Delay(100); // Small delay to reduce CPU usage
        }
    }

    /// <summary>
    /// Sets a new message tag for subsequent logged messages.
    /// </summary>
    private static void SetMessageTag()
    {
        Console.Write("\nEnter message tag/description: ");
        var tag = Console.ReadLine() ?? string.Empty;
        
        lock (_tagLock)
        {
            _currentMessageTag = tag;
        }
        
        Console.WriteLine($"✓ Message tag set to: '{tag}'");
        Console.WriteLine();
    }

    /// <summary>
    /// Clears the current message tag.
    /// </summary>
    private static void ClearMessageTag()
    {
        lock (_tagLock)
        {
            _currentMessageTag = string.Empty;
        }
        
        Console.WriteLine("\n✓ Message tag cleared");
        Console.WriteLine();
    }

    /// <summary>
    /// Sends a canned response defined in configuration.
    /// </summary>
    private static async Task SendCannedResponseAsync(string responseKey)
    {
        var hexString = _configuration?.GetValue<string>($"CannedResponses:{responseKey}");
        
        if (string.IsNullOrEmpty(hexString))
        {
            Console.WriteLine($"\n✗ Canned response '{responseKey}' not found in configuration");
            return;
        }
        
        await SendHexStringAsync(hexString, $"Canned response: {responseKey}");
    }

    /// <summary>
    /// Prompts user for custom hex string and sends it as a response.
    /// </summary>
    private static async Task SendCustomHexResponseAsync()
    {
        Console.Write("\nEnter hex string (e.g., AB0C010203): ");
        var hexString = Console.ReadLine() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(hexString))
        {
            Console.WriteLine("✗ No hex string provided");
            return;
        }
        
        await SendHexStringAsync(hexString, "Custom hex response");
    }

    /// <summary>
    /// Converts a hex string to bytes and sends it via Bluetooth notification.
    /// </summary>
    private static async Task SendHexStringAsync(string hexString, string description)
    {
        try
        {
            // Remove any spaces or separators
            hexString = hexString.Replace(" ", "").Replace("-", "").Replace(":", "");
            
            // Validate hex string
            if (hexString.Length % 2 != 0)
            {
                Console.WriteLine("\n✗ Invalid hex string: Must have an even number of characters");
                return;
            }
            
            // Convert to bytes
            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            
            // Send via Bluetooth
            if (_peripheral != null && _peripheral.IsConnected)
            {
                await _peripheral.SendNotificationAsync(bytes);
                
                Console.WriteLine($"\n✓ Sent {description}:");
                Console.WriteLine($"  Hex: {BitConverter.ToString(bytes).Replace("-", " ")}");
                Console.WriteLine($"  Bytes: {bytes.Length}");
                Console.WriteLine();
                
                _logger?.LogInformation($"Sent response: {description}, {bytes.Length} bytes");
            }
            else
            {
                Console.WriteLine("\n✗ Cannot send response: No active connection");
            }
        }
        catch (FormatException)
        {
            Console.WriteLine("\n✗ Invalid hex string format");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error sending response: {ex.Message}");
            _logger?.LogError(ex, "Error sending Bluetooth response");
        }
    }

    /// <summary>
    /// Event handler for received messages from the controller.
    /// </summary>
    private static void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        string currentTag;
        lock (_tagLock)
        {
            currentTag = _currentMessageTag;
        }
        
        // Log to CSV
        _messageLogger?.LogMessage(e.Timestamp, e.Data, currentTag);
        
        // Display to console
        Console.WriteLine($"\n[{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] Received message:");
        Console.WriteLine($"  Tag: {(string.IsNullOrEmpty(currentTag) ? "(none)" : currentTag)}");
        Console.WriteLine($"  Hex: {BitConverter.ToString(e.Data).Replace("-", " ")}");
        Console.WriteLine($"  Bytes: {e.Data.Length}");
        Console.WriteLine($"  Text: {GetPrintableText(e.Data)}");
        Console.WriteLine();
        
        _logger?.LogInformation($"Received message: {e.Data.Length} bytes, tag: {currentTag}");
    }

    /// <summary>
    /// Event handler for connection status changes.
    /// </summary>
    private static void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Connection status: {e.Status}");
        
        if (!string.IsNullOrEmpty(e.DeviceId))
        {
            Console.WriteLine($"  Device: {e.DeviceId}");
        }
        
        Console.WriteLine();
        
        _logger?.LogInformation($"Connection status changed: {e.Status}, Device: {e.DeviceId}");
    }

    /// <summary>
    /// Converts byte array to printable text representation.
    /// Non-printable characters are shown as dots.
    /// </summary>
    private static string GetPrintableText(byte[] data)
    {
        var chars = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            chars[i] = data[i] >= 32 && data[i] <= 126 
                ? (char)data[i] 
                : '.';
        }
        return new string(chars);
    }
}
