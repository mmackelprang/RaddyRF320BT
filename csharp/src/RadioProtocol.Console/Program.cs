using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadioProtocol.Core;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Models;
using Serilog;

namespace RadioProtocol.Console;

/// <summary>
/// Console application demonstrating the Radio Protocol library
/// </summary>
class Program
{
    private static IRadioManager? _radioManager;
    private static ILogger<Program>? _logger;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    private static String _address = String.Empty;

    static async Task Main(string[] args)
    {
        System.Console.WriteLine("Radio Protocol Library - Console Demo");
        System.Console.WriteLine("=====================================");
        System.Console.WriteLine();

        try
        {
            // Build host with dependency injection
            var host = CreateHostBuilder(args).Build();
            
            // Get services
            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            
            // Initialize radio manager
            _radioManager = new RadioManagerBuilder()
                .WithLogger(host.Services.GetRequiredService<IRadioLogger>())
                .Build();

            // Setup event handlers
            SetupEventHandlers();

            // Handle Ctrl+C gracefully
            System.Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cancellationTokenSource.Cancel();
            };

            _logger.LogInformation("Console application started");

            // Run the main demo
            await RunDemoAsync(configuration);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            _logger?.LogError(ex, "Fatal error in console application");
        }
        finally
        {
            _radioManager?.Dispose();
            _cancellationTokenSource.Dispose();
        }

        System.Console.WriteLine("\nPress any key to exit...");
        System.Console.ReadKey();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: "logs/radio-protocol-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 2,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                ))
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IRadioLogger, RadioLogger>();
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
            });

    private static void SetupEventHandlers()
    {
        if (_radioManager == null) return;

        _radioManager.ConnectionStateChanged += (sender, connectionInfo) =>
        {
            var color = connectionInfo.State switch
            {
                ConnectionState.Connected => ConsoleColor.Green,
                ConnectionState.Connecting => ConsoleColor.Yellow,
                ConnectionState.Error => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };

            WriteColorLine($"Connection: {connectionInfo.State}", color);
            if (!string.IsNullOrEmpty(connectionInfo.ErrorMessage))
            {
                WriteColorLine($"  Error: {connectionInfo.ErrorMessage}", ConsoleColor.Red);
            }
        };

        _radioManager.MessageReceived += (sender, packet) =>
        {
//            WriteColorLine($"Received: {packet.PacketType} - {packet.HexData}", ConsoleColor.Cyan);
//            if (packet.ParsedData != null)
//            {
//                WriteColorLine($"  Parsed: {packet.ParsedData}", ConsoleColor.Blue);
//            }
        };

        _radioManager.StatusUpdated += (sender, status) =>
        {
            WriteColorLine($"Status Update: Freq={status.Frequency}, Vol={status.VolumeLevel}, Squelch={status.SquelchLevel}", ConsoleColor.Magenta);
        };

        _radioManager.DeviceInfoReceived += (sender, deviceInfo) =>
        {
            WriteColorLine($"Device Info: {deviceInfo.RadioVersion} - {deviceInfo.ModelName}", ConsoleColor.Green);
        };
    }

    private static void PrintCommands()
    {
        System.Console.WriteLine("Available Commands:");
        System.Console.WriteLine("  connect [address] - Connect to radio device");
        System.Console.WriteLine("  scan              - Show devices visible");
        System.Console.WriteLine("  disconnect        - Disconnect from device");
        System.Console.WriteLine("  handshake         - Send handshake command");
        System.Console.WriteLine("  number <0-9>      - Press number button");
        System.Console.WriteLine("  number <0-9> long - Press number button (long)");
        System.Console.WriteLine("  volume up/down    - Adjust volume");
        System.Console.WriteLine("  nav up/down       - Navigate up/down");
        System.Console.WriteLine("  nav up/down long  - Navigate up/down (long press)");
        System.Console.WriteLine("  button <type>     - Press specific button");
        System.Console.WriteLine("  demo              - Run automated demo");
        System.Console.WriteLine("  test              - Run test sequence");
        System.Console.WriteLine("  status            - Show current status");
        System.Console.WriteLine("  help              - Show this help");
        System.Console.WriteLine("  quit              - Exit application");
        System.Console.WriteLine();
    }

    private static async Task RunDemoAsync(IConfiguration configuration)
    {
        var deviceAddress = configuration["RadioSettings:DeviceAddress"] ?? "00:11:22:33:44:55";
        var autoConnect = configuration.GetValue<bool>("RadioSettings:AutoConnect");

        PrintCommands();

        if (autoConnect)
        {
            await ConnectToDevice(deviceAddress);
        }

        // Command loop
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            System.Console.Write("> ");
            var input = System.Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();

            try
            {
                await ProcessCommandAsync(command, parts.Skip(1).ToArray());
            }
            catch (Exception ex)
            {
                WriteColorLine($"Error: {ex.Message}", ConsoleColor.Red);
                _logger?.LogError(ex, "Command execution error");
            }
        }
    }

    private static async Task ProcessCommandAsync(string command, string[] args)
    {
        if (_radioManager == null) return;

        switch (command)
        {
            case "connect":
                var address = args.Length > 0 ? args[0] : "00:11:22:33:44:55";
                if(_address != String.Empty)
                {
                    address = _address;
                }
                await ConnectToDevice(address);
                break;

            case "scan":
                await ScanForDevices();
                break;

            case "disconnect":
                await _radioManager.DisconnectAsync();
                break;

            case "handshake":
                var handshakeResult = await _radioManager.SendHandshakeAsync(_cancellationTokenSource.Token);
                WriteCommandResult(handshakeResult);
                break;

            case "number":
                await ProcessNumberCommand(args);
                break;

            case "volume":
                await ProcessVolumeCommand(args);
                break;

            case "nav":
                await ProcessNavigationCommand(args);
                break;

            case "button":
                await ProcessButtonCommand(args);
                break;

            case "demo":
                await RunAutomatedDemo();
                break;

            case "test":
                await RunTestSequence();
                break;

            case "status":
                ShowCurrentStatus();
                break;

            case "help":
                PrintCommands();
                break;

            case "quit":
            case "exit":
                _cancellationTokenSource.Cancel();
                break;

            default:
                WriteColorLine($"Unknown command: {command}. Type 'help' for available commands.", ConsoleColor.Yellow);
                break;
        }
    }

    private static async Task ScanForDevices()
    {
        if (_radioManager == null) return;

        WriteColorLine($"Scanning for devices:", ConsoleColor.Yellow);
        var devices = await _radioManager.ScanForDevicesAsync(_cancellationTokenSource.Token);

        foreach (var d in devices)
        {
            if (d.Name.Contains("RF320"))
            {
                _address = d.Address;
                WriteColorLine($"  {d.Name} - {d.Address}", ConsoleColor.Green);

            }
            else
            {
                WriteColorLine($"  {d.Name} - {d.Address}", ConsoleColor.Gray);
            }
        }
    }

    private static async Task ConnectToDevice(string deviceAddress)
    {
        if (_radioManager == null) return;

        WriteColorLine($"Connecting to device: {deviceAddress}", ConsoleColor.Yellow);
        var success = await _radioManager.ConnectAsync(deviceAddress, _cancellationTokenSource.Token);

        if (success)
        {
            WriteColorLine("Connected successfully!", ConsoleColor.Green);
        }
        else
        {
            WriteColorLine("Connection failed!", ConsoleColor.Red);
        }
    }

    private static async Task ProcessNumberCommand(string[] args)
    {
        if (_radioManager == null || args.Length == 0) return;

        if (!int.TryParse(args[0], out var number) || number < 0 || number > 9)
        {
            WriteColorLine("Invalid number. Use 0-9.", ConsoleColor.Red);
            return;
        }

        var longPress = args.Length > 1 && args[1].ToLowerInvariant() == "long";
        var result = await _radioManager.PressNumberAsync(number, longPress, _cancellationTokenSource.Token);
        WriteCommandResult(result);
    }

    private static async Task ProcessVolumeCommand(string[] args)
    {
        if (_radioManager == null || args.Length == 0) return;

        var direction = args[0].ToLowerInvariant();
        if (direction != "up" && direction != "down")
        {
            WriteColorLine("Invalid volume direction. Use 'up' or 'down'.", ConsoleColor.Red);
            return;
        }

        var result = await _radioManager.AdjustVolumeAsync(direction == "up", _cancellationTokenSource.Token);
        WriteCommandResult(result);
    }

    private static async Task ProcessNavigationCommand(string[] args)
    {
        if (_radioManager == null || args.Length == 0) return;

        var direction = args[0].ToLowerInvariant();
        if (direction != "up" && direction != "down")
        {
            WriteColorLine("Invalid navigation direction. Use 'up' or 'down'.", ConsoleColor.Red);
            return;
        }

        var longPress = args.Length > 1 && args[1].ToLowerInvariant() == "long";
        var result = await _radioManager.NavigateAsync(direction == "up", longPress, _cancellationTokenSource.Token);
        WriteCommandResult(result);
    }

    private static async Task ProcessButtonCommand(string[] args)
    {
        if (_radioManager == null || args.Length == 0) return;

        var buttonName = args[0].ToLowerInvariant();
        if (!Enum.TryParse<ButtonType>(buttonName, true, out var buttonType))
        {
            WriteColorLine($"Invalid button type: {buttonName}", ConsoleColor.Red);
            WriteColorLine("Available buttons: " + string.Join(", ", Enum.GetNames<ButtonType>()), ConsoleColor.Gray);
            return;
        }

        var result = await _radioManager.PressButtonAsync(buttonType, _cancellationTokenSource.Token);
        WriteCommandResult(result);
    }

    private static async Task RunAutomatedDemo()
    {
        if (_radioManager == null) return;

        WriteColorLine("Running automated demo...", ConsoleColor.Green);

        var commands = new (string name, Func<Task<RadioProtocol.Core.Models.CommandResult>> commandFunc)[]
        {
            ("Handshake", () => _radioManager.SendHandshakeAsync(_cancellationTokenSource.Token)),
            ("Number 1", () => _radioManager.PressNumberAsync(1, false, _cancellationTokenSource.Token)),
            ("Number 2", () => _radioManager.PressNumberAsync(2, false, _cancellationTokenSource.Token)),
            ("Volume Up", () => _radioManager.AdjustVolumeAsync(true, _cancellationTokenSource.Token)),
            ("Volume Down", () => _radioManager.AdjustVolumeAsync(false, _cancellationTokenSource.Token)),
            ("Navigate Up", () => _radioManager.NavigateAsync(true, false, _cancellationTokenSource.Token)),
            ("Navigate Down", () => _radioManager.NavigateAsync(false, false, _cancellationTokenSource.Token)),
            ("Band Button", () => _radioManager.PressButtonAsync(ButtonType.Band, _cancellationTokenSource.Token)),
            ("Power Button", () => _radioManager.PressButtonAsync(ButtonType.Power, _cancellationTokenSource.Token))
        };

        foreach (var (name, commandFunc) in commands)
        {
            WriteColorLine($"Executing: {name}", ConsoleColor.Yellow);
            var result = await commandFunc();
            WriteCommandResult(result);
            await Task.Delay(1000, _cancellationTokenSource.Token); // Delay between commands
        }

        WriteColorLine("Demo completed!", ConsoleColor.Green);
    }

    private static async Task RunTestSequence()
    {
        if (_radioManager == null) return;

        WriteColorLine("Running test sequence based on documented messages...", ConsoleColor.Green);

        // Test sequence based on COMMAND_RESPONSE_SEQUENCES.md
        var testCommands = new[]
        {
            ButtonType.Band,
            ButtonType.Number1,
            ButtonType.Number2,
            ButtonType.Number3,
            ButtonType.VolumeUp,
            ButtonType.VolumeDown,
            ButtonType.UpShort,
            ButtonType.DownShort,
            ButtonType.Frequency,
            ButtonType.Memo,
            ButtonType.Record
        };

        foreach (var button in testCommands)
        {
            WriteColorLine($"Testing button: {button}", ConsoleColor.Yellow);
            var result = await _radioManager.PressButtonAsync(button, _cancellationTokenSource.Token);
            WriteCommandResult(result);
            await Task.Delay(500, _cancellationTokenSource.Token);
        }

        WriteColorLine("Test sequence completed!", ConsoleColor.Green);
    }

    private static void ShowCurrentStatus()
    {
        if (_radioManager == null) return;

        WriteColorLine("Current Status:", ConsoleColor.White);
        WriteColorLine($"  Connected: {_radioManager.IsConnected}", ConsoleColor.Gray);
        WriteColorLine($"  Connection: {_radioManager.ConnectionStatus.State}", ConsoleColor.Gray);
        
        if (_radioManager.CurrentStatus != null)
        {
            var status = _radioManager.CurrentStatus;
            WriteColorLine($"  Frequency: {status.Frequency}", ConsoleColor.Gray);
            WriteColorLine($"  Band: {status.Band}", ConsoleColor.Gray);
            WriteColorLine($"  Volume: {status.VolumeLevel}", ConsoleColor.Gray);
            WriteColorLine($"  Squelch: {status.SquelchLevel}", ConsoleColor.Gray);
            WriteColorLine($"  Stereo: {status.IsStereo}", ConsoleColor.Gray);
            WriteColorLine($"  Power: {status.IsPowerOn}", ConsoleColor.Gray);
        }

        if (_radioManager.DeviceInformation != null)
        {
            var device = _radioManager.DeviceInformation;
            WriteColorLine($"  Device: {device.RadioVersion} - {device.ModelName}", ConsoleColor.Gray);
        }
    }

    private static void WriteCommandResult(CommandResult result)
    {
        var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        WriteColorLine($"Command {(result.Success ? "succeeded" : "failed")} in {result.ExecutionTime.TotalMilliseconds:F1}ms", color);
        
        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            WriteColorLine($"  Error: {result.ErrorMessage}", ConsoleColor.Red);
        }

        if (result.SentData != null)
        {
            WriteColorLine($"  Sent: {Convert.ToHexString(result.SentData)}", ConsoleColor.Gray);
        }

        if (result.Response != null)
        {
            WriteColorLine($"  Response: {result.Response.PacketType} - {result.Response.HexData}", ConsoleColor.Blue);
        }
    }

    private static void WriteColorLine(string text, ConsoleColor color)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(text);
        System.Console.ForegroundColor = originalColor;
    }
}