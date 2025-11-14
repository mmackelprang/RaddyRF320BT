using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadioProtocol.Core;
using RadioProtocol.Core.Bluetooth;
using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Protocol;
using Spectre.Console;

namespace RadioProtocol.Console;

/// <summary>
/// RF320 Radio Protocol Console Application
/// Interactive keyboard control and command-line automation
/// </summary>
class Program
{
    private static IRadioLogger? _logger;
    private static RadioConnection? _radio;
    private static IRadioTransport? _transport;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // Status tracking for display
    private static string _currentBand = "---";
    private static string _currentFrequency = "---";
    private static string _currentVolume = "---";
    private static int _currentSignal = 0;
    private static DateTime _lastStatusUpdate = DateTime.MinValue;

    static async Task<int> Main(string[] args)
    {
        // Check for command-line mode
        var commandLineMode = args.Length > 0;
        var commandsToSend = new List<CanonicalAction>();

        if (commandLineMode)
        {
            // Parse command-line arguments as action names
            foreach (var arg in args)
            {
                if (Enum.TryParse<CanonicalAction>(arg, true, out var action))
                {
                    commandsToSend.Add(action);
                }
                else
                {
                    System.Console.WriteLine($"Unknown action: {arg}");
                    System.Console.WriteLine("Valid actions: " + string.Join(", ", Enum.GetNames<CanonicalAction>()));
                    return 1;
                }
            }
        }

        // Setup logger
        var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadioProtocol", "Logs");
        System.IO.Directory.CreateDirectory(logDir);
        var logFile = System.IO.Path.Combine(logDir, $"RadioProtocol_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        
        var simpleLogger = new SimpleFileLogger(logFile);
        _logger = new RadioLogger(simpleLogger);

        // Handle Ctrl+C gracefully
        System.Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            _cancellationTokenSource.Cancel();
        };

        // Display header
        AnsiConsole.Clear();
        
        if (commandLineMode)
        {
            AnsiConsole.Write(new Rule($"[bold blue]RF320 Command-Line Mode ({commandsToSend.Count} commands)[/]").Centered());
        }
        else
        {
            AnsiConsole.Write(new Rule("[bold blue]RF320 Radio Protocol Console[/]").Centered());
        }
        
        AnsiConsole.WriteLine();
        
        _logger.LogInfo("Application started");
        _logger.LogInfo($"Log file: {logFile}");

        try
        {
            // Scan for RF320 devices
            _logger.LogInfo("Scanning for RF320 devices...");
            
            var bluetoothConnection = BluetoothConnectionFactory.Create(_logger);
            
            IEnumerable<DeviceInfo> devices = Array.Empty<DeviceInfo>();
            
            await AnsiConsole.Status()
                .StartAsync("Scanning for RF320 devices...", async ctx =>
                {
                    devices = await bluetoothConnection.ScanForDevicesAsync(_cancellationTokenSource.Token);
                });
            
            var rf320Device = devices.FirstOrDefault(d => d.Name.Contains("RF320", StringComparison.OrdinalIgnoreCase));
            
            if (rf320Device == null)
            {
                AnsiConsole.MarkupLine("[red]No RF320 device found. Exiting.[/]");
                _logger.LogError(null, "No RF320 device found");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Found device: {rf320Device.Name} ({rf320Device.Address})[/]");
            _logger.LogInfo($"Found device: {rf320Device.Name} (Address: {rf320Device.Address})");
            AnsiConsole.WriteLine();

            // Connect to device
            bool connected = false;
            await AnsiConsole.Status()
                .StartAsync("Connecting to device...", async ctx =>
                {
                    connected = await bluetoothConnection.ConnectAsync(rf320Device.Address, _cancellationTokenSource.Token);
                });
            
            if (!connected)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to device.[/]");
                _logger.LogError(null, "Failed to connect to device");
                return 1;
            }

            AnsiConsole.MarkupLine("[green]Connected successfully![/]");
            _logger.LogInfo("Connected to device successfully");
            AnsiConsole.WriteLine();

            // Create transport adapter
            _transport = new TransportAdapter(bluetoothConnection);
            
            // Create radio connection
            _radio = new RadioConnection(_transport, _logger);
            
            // Set up event handlers
            SetupEventHandlers();

            // Initialize radio
            bool initialized = false;
            await AnsiConsole.Status()
                .StartAsync("Initializing radio (handshake)...", async ctx =>
                {
                    _logger.LogInfo("Sending handshake");
                    initialized = await _radio.InitializeAsync(_cancellationTokenSource.Token);
                    if (!initialized)
                    {
                        _logger.LogInfo("No handshake response (expected - device streams status instead)");
                    }
                });

            AnsiConsole.MarkupLine("[green]Radio initialized successfully![/]");
            _logger.LogInfo("Handshake complete - status stream active");
            AnsiConsole.WriteLine();

            if (commandLineMode)
            {
                // Command-line mode: Send specified commands and exit
                await RunCommandLineMode(commandsToSend);
            }
            else
            {
                // Interactive mode
                await RunInteractiveMode();
            }
            
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("\n[yellow]Operation cancelled.[/]");
            _logger?.LogInfo("Operation cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"\n[red]Error: {ex.Message}[/]");
            _logger?.LogError(ex, $"Unhandled exception: {ex.Message}");
            return 1;
        }
        finally
        {
            _radio?.Dispose();
            _transport?.Dispose();
            _logger?.LogInfo("Application exiting");
            
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[dim]Session Complete[/]"));
            AnsiConsole.MarkupLine($"[dim]Log file saved to: {logFile}[/]");
        }
    }

    private static void SetupEventHandlers()
    {
        if (_radio == null) return;

        _radio.StatusReceived += (s, status) =>
        {
            _lastStatusUpdate = DateTime.Now;
            _logger?.LogInfo($"Status: {status.Label} = '{status.Value}'");
            
            // Track status for display
            if (status.Label == "VolumeValue") _currentVolume = status.Value;
        };

        _radio.StateUpdated += (s, state) =>
        {
            _lastStatusUpdate = DateTime.Now;
            _currentBand = state.BandName;
            // Format frequency based on band: MW has no decimals, FM has 2, others have 3
            string freqFormat = state.BandCode switch
            {
                0x00 => "0.00",  // FM: 2 decimal places
                0x01 => "0",     // MW: 0 decimal places
                _ => "0.000"     // All others: 3 decimal places
            };
            _currentFrequency = $"{state.FrequencyMHz.ToString(freqFormat)} {(state.UnitIsMHz ? "MHz" : "KHz")}";
            _currentSignal = state.SignalStrength;
            
            _logger?.LogInfo($"State: Band={state.BandName} Freq={state.FrequencyMHz.ToString(freqFormat)} {(state.UnitIsMHz ? "MHz" : "KHz")} Signal={state.SignalStrength}/6");
        };

        _radio.FrameReceived += (s, frame) =>
        {
            // Only log non-status frames (status floods the log)
            if (frame.Group != CommandGroup.Status)
            {
                _logger?.LogInfo($"Frame: Group={frame.Group} CommandId=0x{frame.CommandId:X2}");
            }
        };
    }

    private static async Task RunCommandLineMode(List<CanonicalAction> commands)
    {
        if (_radio == null) return;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]Status[/]");

        AnsiConsole.MarkupLine($"[yellow]Sending {commands.Count} command(s)...[/]");
        AnsiConsole.WriteLine();
        
        foreach (var action in commands)
        {
            _logger?.LogInfo($"Command-line mode: Sending {action}");
            
            var success = await _radio.SendAsync(action);
            
            table.AddRow(
                action.ToString(),
                success ? "[green]✓ Sent[/]" : "[red]✗ Failed[/]"
            );
            
            if (!success)
            {
                _logger?.LogError(null, $"Failed to send {action}");
            }
            
            // Small delay between commands
            await Task.Delay(100);
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        
        await AnsiConsole.Status()
            .StartAsync("Waiting 5 seconds for responses...", async ctx =>
            {
                await Task.Delay(5000, _cancellationTokenSource.Token);
            });
        
        AnsiConsole.MarkupLine("[green]Done. Check log file for all messages.[/]");
    }

    private static async Task RunInteractiveMode()
    {
        if (_radio == null) return;

        // Clear screen and show interface
        AnsiConsole.Clear();
        PrintStatusHeader();
        
        var panel = new Panel(KeyboardMapper.GetKeyboardHelp())
            .Border(BoxBorder.Rounded)
            .Header("[yellow]Keyboard Commands[/]")
            .BorderColor(Color.Yellow);
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Ready! Press keys to send commands, ESC to exit.[/]");
        
        // Start background status updater
        _ = Task.Run(async () => await StatusUpdateLoop());

        // Main keyboard loop
        await KeyboardLoopAsync();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Shutting down...[/]");
        _logger?.LogInfo("Shutting down");
    }

    private static void PrintStatusHeader()
    {
        var cursorTop = System.Console.CursorTop;
        System.Console.SetCursorPosition(0, 0);
        
        var signalBars = new string('█', _currentSignal) + new string('░', 6 - _currentSignal);
        
        var table = new Table()
            .Border(TableBorder.Heavy)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold]Band[/]").Centered())
            .AddColumn(new TableColumn("[bold]Frequency[/]").Centered())
            .AddColumn(new TableColumn("[bold]Volume[/]").Centered())
            .AddColumn(new TableColumn("[bold]Signal[/]").Centered());
        
        table.AddRow(
            $"[cyan]{_currentBand}[/]",
            $"[cyan]{_currentFrequency}[/]",
            $"[cyan]{_currentVolume}[/]",
            $"[cyan]{signalBars}[/]"
        );
        
        AnsiConsole.Write(table);
        
        // Restore cursor position if we were below the header
        if (cursorTop > 4)
        {
            System.Console.SetCursorPosition(0, cursorTop);
        }
    }

    private static async Task StatusUpdateLoop()
    {
        var lastPrintedStatus = string.Empty;
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var currentStatus = $"{_currentBand}|{_currentFrequency}|{_currentVolume}|{_currentSignal}";
            
            // Only update if status has changed
            if (currentStatus != lastPrintedStatus)
            {
                PrintStatusHeader();
                lastPrintedStatus = currentStatus;
            }
            
            await Task.Delay(500);
        }
    }

    private static async Task KeyboardLoopAsync()
    {
        if (_radio == null) return;

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (!System.Console.KeyAvailable)
            {
                await Task.Delay(50, _cancellationTokenSource.Token);
                continue;
            }

            var keyInfo = System.Console.ReadKey(intercept: true);

            // Exit on Escape
            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _logger?.LogInfo("User pressed ESC to exit");
                break;
            }

            // Try to map key to action
            if (KeyboardMapper.TryGetAction(keyInfo, out var action))
            {
                try
                {
                    _logger?.LogInfo($"Keyboard: {keyInfo.Key} → {action}");
                    
                    var success = await _radio.SendAsync(action);
                    
                    if (!success)
                    {
                        _logger?.LogError(null, $"Failed to send {action}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error sending {action}");
                }
            }
        }
    }
}

/// <summary>
/// Adapter to wrap IBluetoothConnection as IRadioTransport
/// </summary>
internal class TransportAdapter : IRadioTransport
{
    private readonly IBluetoothConnection _connection;

    public event EventHandler<byte[]>? NotificationReceived;

    public TransportAdapter(IBluetoothConnection connection)
    {
        _connection = connection;
        _connection.DataReceived += (s, data) => NotificationReceived?.Invoke(this, data);
    }

    public Task<bool> WriteAsync(byte[] data)
    {
        return _connection.SendDataAsync(data);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

/// <summary>
/// Simple file logger that implements ILogger
/// </summary>
internal class SimpleFileLogger : ILogger<RadioLogger>
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public SimpleFileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        WriteHeader();
    }

    private void WriteHeader()
    {
        lock (_lock)
        {
            System.IO.File.AppendAllText(_logFilePath, 
                $"=== Radio Protocol Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===\n\n");
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            System.IO.File.AppendAllText(_logFilePath, $"[{timestamp}] [{logLevel}] {message}\n");
            
            if (exception != null)
            {
                System.IO.File.AppendAllText(_logFilePath, $"  Exception: {exception}\n");
            }
        }
    }
}
