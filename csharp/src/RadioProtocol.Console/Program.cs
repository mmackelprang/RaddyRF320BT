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
using BluetoothDeviceInfo = RadioProtocol.Core.Bluetooth.DeviceInfo;

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
    private static RadioProtocolParser? _parser;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // Status tracking for display
    private static string _currentBand = "---";
    private static string _currentFrequency = "---";
    private static string _currentVolume = "---";
    private static int _currentSignal = 0;
    private static DateTime _lastStatusUpdate = DateTime.MinValue;
    
    // New state information for display
    private static string _lastTextMessage = "---";
    private static string _demodulationValue = "---";
    private static string _bandwidthValue = "---";
    private static string _equalizerType = "---";
    private static string _modelVersion = "---";
    private static string _modelVersionNumber = "---";
    private static string _radioVersion = "---";
    private static string _radioVersionNumber = "---";

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
            
            IEnumerable<BluetoothDeviceInfo> devices = Array.Empty<BluetoothDeviceInfo>();
            
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
            
            // Create parser for message parsing
            _parser = new RadioProtocolParser(_logger);
            
            // Subscribe to raw data for additional parsing
            if (_transport is TransportAdapter adapter)
            {
                adapter.RawDataReceived += OnRawDataReceived;
            }
            
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
            if (status.Label == "Demodulation") _demodulationValue = status.Value;
            if (status.Label == "BandWidth") _bandwidthValue = status.Value;
            if (status.Label == "Model") 
            {
                _modelVersion = status.Value;
                // Try to extract version number if it's in format like "Model: 320"
                var parts = status.Value.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var modelNum))
                {
                    _modelVersionNumber = modelNum.ToString();
                }
            }
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

    private static void OnRawDataReceived(object? sender, byte[] data)
    {
        if (_parser == null) return;
        
        try
        {
            var packet = _parser.ParseReceivedData(data);
            
            if (packet.IsValid && packet.ParsedData != null)
            {
                switch (packet.ParsedData)
                {
                    case RadioProtocol.Core.Models.TextMessageInfo textMsg when textMsg.IsComplete:
                        _lastTextMessage = textMsg.Message;
                        _logger?.LogInfo($"Text Message: {textMsg.Message}");
                        break;
                        
                    case RadioProtocol.Core.Models.EqualizerInfo eqInfo:
                        _equalizerType = eqInfo.Text ?? eqInfo.EqualizerType.ToString();
                        _logger?.LogInfo($"Equalizer: {_equalizerType}");
                        break;
                        
                    case RadioProtocol.Core.Models.ModelInfo modelInfo:
                        _modelVersionNumber = modelInfo.VersionNumber.ToString();
                        _modelVersion = modelInfo.VersionText ?? $"Model {modelInfo.VersionNumber}";
                        _logger?.LogInfo($"Model Info: {_modelVersion} (#{_modelVersionNumber})");
                        break;
                        
                    case RadioProtocol.Core.Models.RadioVersionInfo radioInfo:
                        _radioVersionNumber = radioInfo.VersionNumber.ToString();
                        _radioVersion = radioInfo.VersionText ?? $"Radio {radioInfo.VersionNumber}";
                        _logger?.LogInfo($"Radio Version: {_radioVersion} (#{_radioVersionNumber})");
                        break;
                        
                    case RadioProtocol.Core.Models.DemodulationInfo demodInfo:
                        _demodulationValue = demodInfo.Text ?? demodInfo.Value.ToString();
                        break;
                        
                    case RadioProtocol.Core.Models.BandwidthInfo bwInfo:
                        _bandwidthValue = bwInfo.Text ?? bwInfo.Value.ToString();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing raw data");
        }
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
        
        // Main status table (existing info)
        var mainTable = new Table()
            .Border(TableBorder.Heavy)
            .BorderColor(Color.Blue)
            .AddColumn(new TableColumn("[bold]Band[/]").Centered())
            .AddColumn(new TableColumn("[bold]Frequency[/]").Centered())
            .AddColumn(new TableColumn("[bold]Volume[/]").Centered())
            .AddColumn(new TableColumn("[bold]Signal[/]").Centered());
        
        mainTable.AddRow(
            $"[cyan]{_currentBand}[/]",
            $"[cyan]{_currentFrequency}[/]",
            $"[cyan]{_currentVolume}[/]",
            $"[cyan]{signalBars}[/]"
        );
        
        AnsiConsole.Write(mainTable);
        
        // Extended status table (new info) - split into two rows for compactness
        var extendedTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[bold]Demod[/]").Centered())
            .AddColumn(new TableColumn("[bold]Bandwidth[/]").Centered())
            .AddColumn(new TableColumn("[bold]Equalizer[/]").Centered())
            .AddColumn(new TableColumn("[bold]Model[/]").Centered());
        
        extendedTable.AddRow(
            $"[yellow]{_demodulationValue}[/]",
            $"[yellow]{_bandwidthValue}[/]",
            $"[yellow]{_equalizerType}[/]",
            $"[yellow]{_modelVersion}[/]"
        );
        
        AnsiConsole.Write(extendedTable);
        
        // Second row of extended info
        var extendedTable2 = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn(new TableColumn("[bold]Model Ver#[/]").Centered())
            .AddColumn(new TableColumn("[bold]Radio Ver[/]").Centered())
            .AddColumn(new TableColumn("[bold]Radio Ver#[/]").Centered())
            .AddColumn(new TableColumn("[bold]Last Text[/]").LeftAligned());
        
        // Truncate text message if too long
        var truncatedText = _lastTextMessage.Length > 30 ? _lastTextMessage.Substring(0, 27) + "..." : _lastTextMessage;
        
        extendedTable2.AddRow(
            $"[yellow]{_modelVersionNumber}[/]",
            $"[yellow]{_radioVersion}[/]",
            $"[yellow]{_radioVersionNumber}[/]",
            $"[yellow]{truncatedText}[/]"
        );
        
        AnsiConsole.Write(extendedTable2);
        
        // Restore cursor position if we were below the header
        if (cursorTop > 9)  // Increased from 4 to account for new tables
        {
            System.Console.SetCursorPosition(0, cursorTop);
        }
    }

    private static async Task StatusUpdateLoop()
    {
        var lastPrintedStatus = string.Empty;
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var currentStatus = $"{_currentBand}|{_currentFrequency}|{_currentVolume}|{_currentSignal}|" +
                               $"{_lastTextMessage}|{_demodulationValue}|{_bandwidthValue}|{_equalizerType}|" +
                               $"{_modelVersion}|{_modelVersionNumber}|{_radioVersion}|{_radioVersionNumber}";
            
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
    public event EventHandler<byte[]>? RawDataReceived;  // New event for raw data

    public TransportAdapter(IBluetoothConnection connection)
    {
        _connection = connection;
        _connection.DataReceived += (s, data) => 
        {
            RawDataReceived?.Invoke(this, data);  // Fire raw data event first
            NotificationReceived?.Invoke(this, data);
        };
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
