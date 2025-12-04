# RTest Integration Plan: RadioProtocol.Core to IRadioControls Shim

## Overview

This document outlines a detailed plan to integrate the `RadioProtocol.Core` library into the `RTest` project by creating a shim layer that implements the `IRadioControls` interface defined at `RTest/src/Radio.Core/Interfaces/Audio/IRadioControls.cs`.

## Current State Analysis

### RadioProtocol.Core Library Structure

The existing `RadioProtocol.Core` library provides comprehensive radio control functionality:

```
RadioProtocol.Core/
├── Bluetooth/
│   ├── BluetoothConnection.cs      # IBluetoothConnection interface and factory
│   ├── IRadioTransport.cs          # Low-level transport abstraction
│   ├── LinuxBluetoothConnection.cs # BlueZ implementation
│   └── WindowsBluetoothConnection.cs # Windows BLE implementation
├── Commands/
│   └── RadioCommandBuilder.cs      # Command packet construction
├── Constants/
│   ├── ButtonType.cs               # Button/control enums
│   ├── ConnectionState.cs          # Connection states
│   ├── EqualizerType.cs            # Audio equalizer types
│   ├── MessageType.cs              # Protocol message types
│   ├── ProtocolConstants.cs        # Protocol magic numbers
│   └── ResponsePacketType.cs       # Response packet types
├── Logging/
│   ├── IRadioLogger.cs             # Logging interface
│   ├── RadioLogger.cs              # Logger implementation
│   ├── FileLogger.cs               # File logging
│   └── FileLoggerProvider.cs       # DI provider
├── Messages/
│   ├── BaseMessage.cs              # Message base classes
│   ├── ButtonPressMessage.cs       # Button press commands
│   ├── ChannelCommandMessage.cs    # Channel commands
│   ├── ResponseMessage.cs          # Response parsing
│   ├── StatusRequestMessage.cs     # Status queries
│   └── SyncRequestMessage.cs       # Sync commands
├── Models/
│   ├── AudioInfo.cs                # Volume, signal, stereo info
│   ├── BandwidthInfo.cs            # Bandwidth settings
│   ├── BatteryInfo.cs              # Battery status
│   ├── CommandResult.cs            # Command execution result
│   ├── ConnectionInfo.cs           # Connection status
│   ├── DemodulationInfo.cs         # Demodulation mode
│   ├── DeviceInfo.cs               # Device identification
│   ├── EqualizerInfo.cs            # EQ settings
│   ├── FrequencyInfo.cs            # Frequency/band info
│   ├── MemoryChannelInfo.cs        # Memory presets
│   ├── ModulationInfo.cs           # Modulation settings
│   ├── RadioStatus.cs              # Complete radio state
│   ├── RecordingInfo.cs            # Recording status
│   ├── SNRInfo.cs                  # Signal-to-noise ratio
│   ├── VolInfo.cs                  # Volume info
│   └── ResponsePacket.cs           # Raw response wrapper
├── Protocol/
│   ├── CanonicalAction.cs          # Action enum and mapping
│   ├── ProtocolUtils.cs            # Protocol utilities
│   ├── RadioConnection.cs          # High-level connection manager
│   ├── RadioFrame.cs               # Frame parsing/building
│   ├── RadioProtocolParser.cs      # Protocol parsing
│   └── StatusMessage.cs            # Status message parsing
└── RadioManager.cs                 # Main API (IRadioManager)
```

### Key Existing Interfaces

#### IRadioManager (Main API)
```csharp
public interface IRadioManager : IDisposable
{
    event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    event EventHandler<ResponsePacket>? MessageReceived;
    event EventHandler<RadioStatus>? StatusUpdated;
    event EventHandler<DeviceInfo>? DeviceInfoReceived;
    
    Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceAddress, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<CommandResult> SendCommandAsync(byte[] command, CancellationToken ct = default);
    
    Task<CommandResult> PressButtonAsync(ButtonType buttonType, CancellationToken ct = default);
    Task<CommandResult> PressNumberAsync(int number, bool longPress = false, CancellationToken ct = default);
    Task<CommandResult> AdjustVolumeAsync(bool up, CancellationToken ct = default);
    Task<CommandResult> NavigateAsync(bool up, bool longPress = false, CancellationToken ct = default);
    Task<CommandResult> SendHandshakeAsync(CancellationToken ct = default);
    
    bool IsConnected { get; }
    ConnectionInfo ConnectionStatus { get; }
    RadioStatus? CurrentStatus { get; }
    DeviceInfo? DeviceInformation { get; }
}
```

#### IBluetoothConnection (Transport Layer)
```csharp
public interface IBluetoothConnection : IDisposable
{
    event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    event EventHandler<byte[]>? DataReceived;

    Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken ct = default);
    Task<bool> ConnectAsync(string deviceAddress, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<bool> SendDataAsync(byte[] data, CancellationToken ct = default);
    bool IsConnected { get; }
    ConnectionInfo ConnectionStatus { get; }
}
```

---

## Proposed IRadioControls Interface

Based on common radio control patterns and the capabilities of `RadioProtocol.Core`, here is the proposed `IRadioControls` interface:

```csharp
namespace RTest.Radio.Core.Interfaces.Audio
{
    /// <summary>
    /// Primary interface for radio control operations.
    /// Provides high-level abstraction for audio/radio device control.
    /// </summary>
    public interface IRadioControls : IDisposable
    {
        #region Connection Management
        
        /// <summary>
        /// Connects to the radio device.
        /// </summary>
        /// <param name="deviceIdentifier">Device address or name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync(string deviceIdentifier, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Disconnects from the radio device.
        /// </summary>
        Task DisconnectAsync();
        
        /// <summary>
        /// Gets whether the radio is currently connected.
        /// </summary>
        bool IsConnected { get; }
        
        #endregion

        #region Audio Controls
        
        /// <summary>
        /// Gets the current volume level (0-100).
        /// </summary>
        int Volume { get; }
        
        /// <summary>
        /// Sets the volume level.
        /// </summary>
        /// <param name="level">Volume level (0-100)</param>
        Task SetVolumeAsync(int level);
        
        /// <summary>
        /// Increases volume by one step.
        /// </summary>
        Task VolumeUpAsync();
        
        /// <summary>
        /// Decreases volume by one step.
        /// </summary>
        Task VolumeDownAsync();
        
        /// <summary>
        /// Gets or sets the mute state.
        /// </summary>
        bool IsMuted { get; }
        
        /// <summary>
        /// Toggles mute state.
        /// </summary>
        Task ToggleMuteAsync();
        
        #endregion

        #region Frequency/Tuning Controls
        
        /// <summary>
        /// Gets the current frequency in Hz.
        /// </summary>
        double CurrentFrequency { get; }
        
        /// <summary>
        /// Gets the current frequency as a formatted string.
        /// </summary>
        string CurrentFrequencyDisplay { get; }
        
        /// <summary>
        /// Sets the frequency directly.
        /// </summary>
        /// <param name="frequencyHz">Frequency in Hz</param>
        Task SetFrequencyAsync(double frequencyHz);
        
        /// <summary>
        /// Tunes up to the next frequency step.
        /// </summary>
        Task TuneUpAsync();
        
        /// <summary>
        /// Tunes down to the previous frequency step.
        /// </summary>
        Task TuneDownAsync();
        
        /// <summary>
        /// Gets the current band name (FM, AM, SW, etc.).
        /// </summary>
        string CurrentBand { get; }
        
        /// <summary>
        /// Cycles to the next band.
        /// </summary>
        Task ChangeBandAsync();
        
        #endregion

        #region Preset/Memory Channels
        
        /// <summary>
        /// Recalls a preset memory channel.
        /// </summary>
        /// <param name="presetNumber">Preset number (1-10)</param>
        Task RecallPresetAsync(int presetNumber);
        
        /// <summary>
        /// Saves the current frequency to a preset.
        /// </summary>
        /// <param name="presetNumber">Preset number (1-10)</param>
        Task SavePresetAsync(int presetNumber);
        
        #endregion

        #region Audio Mode Controls
        
        /// <summary>
        /// Gets the current demodulation mode.
        /// </summary>
        string DemodulationMode { get; }
        
        /// <summary>
        /// Cycles through demodulation modes.
        /// </summary>
        Task CycleDemodulationAsync();
        
        /// <summary>
        /// Gets the current bandwidth setting.
        /// </summary>
        string Bandwidth { get; }
        
        /// <summary>
        /// Cycles through bandwidth settings.
        /// </summary>
        Task CycleBandwidthAsync();
        
        /// <summary>
        /// Gets or sets the stereo mode.
        /// </summary>
        bool IsStereo { get; }
        
        /// <summary>
        /// Toggles stereo/mono mode.
        /// </summary>
        Task ToggleStereoAsync();
        
        /// <summary>
        /// Gets the current equalizer type.
        /// </summary>
        string EqualizerType { get; }
        
        /// <summary>
        /// Cycles through equalizer presets.
        /// </summary>
        Task CycleEqualizerAsync();
        
        #endregion

        #region Signal Information
        
        /// <summary>
        /// Gets the current signal strength (0-6).
        /// </summary>
        int SignalStrength { get; }
        
        /// <summary>
        /// Gets the signal-to-noise ratio.
        /// </summary>
        int SNR { get; }
        
        #endregion

        #region Power Controls
        
        /// <summary>
        /// Gets the power state.
        /// </summary>
        bool IsPoweredOn { get; }
        
        /// <summary>
        /// Toggles power state.
        /// </summary>
        Task TogglePowerAsync();
        
        /// <summary>
        /// Powers off the device.
        /// </summary>
        Task PowerOffAsync();
        
        #endregion

        #region Recording Controls
        
        /// <summary>
        /// Gets whether recording is active.
        /// </summary>
        bool IsRecording { get; }
        
        /// <summary>
        /// Toggles recording state.
        /// </summary>
        Task ToggleRecordingAsync();
        
        #endregion

        #region Events
        
        /// <summary>
        /// Raised when connection state changes.
        /// </summary>
        event EventHandler<RadioConnectionChangedEventArgs>? ConnectionChanged;
        
        /// <summary>
        /// Raised when radio status is updated.
        /// </summary>
        event EventHandler<RadioStatusChangedEventArgs>? StatusChanged;
        
        /// <summary>
        /// Raised when volume changes.
        /// </summary>
        event EventHandler<VolumeChangedEventArgs>? VolumeChanged;
        
        /// <summary>
        /// Raised when frequency changes.
        /// </summary>
        event EventHandler<FrequencyChangedEventArgs>? FrequencyChanged;
        
        #endregion
    }
    
    #region Event Args
    
    public class RadioConnectionChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; init; }
        public string? DeviceAddress { get; init; }
        public string? ErrorMessage { get; init; }
    }
    
    public class RadioStatusChangedEventArgs : EventArgs
    {
        public string? Band { get; init; }
        public double FrequencyHz { get; init; }
        public int Volume { get; init; }
        public int SignalStrength { get; init; }
        public bool IsStereo { get; init; }
        public string? DemodulationMode { get; init; }
    }
    
    public class VolumeChangedEventArgs : EventArgs
    {
        public int OldVolume { get; init; }
        public int NewVolume { get; init; }
        public bool IsMuted { get; init; }
    }
    
    public class FrequencyChangedEventArgs : EventArgs
    {
        public double OldFrequencyHz { get; init; }
        public double NewFrequencyHz { get; init; }
        public string? Band { get; init; }
    }
    
    #endregion
}
```

---

## Implementation Plan

> **Note on GitHub Copilot Prompts**: The prompts below use GitHub Copilot's agent commands:
> - `@workspace` - Asks Copilot to work with files and projects in your workspace
> - `@terminal` - Asks Copilot to execute commands in the integrated terminal
> 
> These prompts are designed for use with GitHub Copilot Chat in VS Code or compatible IDEs.

### Phase 1: Create Shim Project Structure

Create a new project `RadioProtocol.RTest.Shim` that bridges `RadioProtocol.Core` to `IRadioControls`:

```
RadioProtocol.RTest.Shim/
├── RadioProtocol.RTest.Shim.csproj
├── Interfaces/
│   └── IRadioControls.cs           # Interface definition (or reference)
├── Adapters/
│   └── RadioControlsAdapter.cs     # Main shim implementation
├── EventArgs/
│   ├── RadioConnectionChangedEventArgs.cs
│   ├── RadioStatusChangedEventArgs.cs
│   ├── VolumeChangedEventArgs.cs
│   └── FrequencyChangedEventArgs.cs
├── Extensions/
│   └── RadioManagerExtensions.ms   # Helper extension methods
└── Utilities/
    └── FrequencyConverter.cs       # Frequency format utilities
```

#### GitHub Copilot Prompts for Phase 1

**Prompt 1.1: Create the project structure**
```
@workspace Create a new .NET 8.0 class library project called RadioProtocol.RTest.Shim in the 
csharp/src directory with the following folder structure:
- Interfaces/
- Adapters/
- EventArgs/
- Extensions/
- Utilities/
- Exceptions/

Add a project reference to RadioProtocol.Core and update the solution file.
```

**Prompt 1.2: Verify project creation**
```
@terminal Run the following commands to verify the project was created correctly:
cd csharp && dotnet build RadioProtocol.RTest.Shim/RadioProtocol.RTest.Shim.csproj
```

**Prompt 1.3: Add test project**
```
@workspace Create a new xUnit test project called RadioProtocol.RTest.Shim.Tests in the 
csharp/tests directory. Add references to RadioProtocol.RTest.Shim, Moq, and FluentAssertions.
Update the solution file to include the test project.
```

**Verification Checklist for Phase 1:**
- [ ] Project `RadioProtocol.RTest.Shim.csproj` exists and builds
- [ ] Project has reference to `RadioProtocol.Core`
- [ ] Test project `RadioProtocol.RTest.Shim.Tests.csproj` exists and builds
- [ ] Solution file includes both new projects
- [ ] All folder structures are in place

### Phase 2: Implement RadioControlsAdapter

The adapter will wrap `IRadioManager` and implement `IRadioControls`:

```csharp
namespace RadioProtocol.RTest.Shim.Adapters
{
    /// <summary>
    /// Adapter that implements IRadioControls using RadioProtocol.Core.IRadioManager
    /// </summary>
    public class RadioControlsAdapter : IRadioControls
    {
        private readonly IRadioManager _radioManager;
        private readonly IRadioLogger _logger;
        private bool _disposed;

        // Cached state from status updates
        private int _volume;
        private bool _isMuted;
        private double _currentFrequency;
        private string _currentBand = "FM";
        private string _demodulationMode = "FM";
        private string _bandwidth = "Auto";
        private bool _isStereo;
        private int _signalStrength;
        private int _snr;
        private bool _isPoweredOn;
        private bool _isRecording;
        private string _equalizerType = "Normal";

        public RadioControlsAdapter(IRadioManager radioManager, IRadioLogger logger)
        {
            _radioManager = radioManager ?? throw new ArgumentNullException(nameof(radioManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to RadioManager events
            _radioManager.ConnectionStateChanged += OnConnectionStateChanged;
            _radioManager.StatusUpdated += OnStatusUpdated;
        }

        // ... Implementation details below
    }
}
```

#### GitHub Copilot Prompts for Phase 2

**Prompt 2.1: Create the IRadioControls interface**
```
@workspace Create the IRadioControls interface in 
csharp/src/RadioProtocol.RTest.Shim/Interfaces/IRadioControls.cs based on the interface 
definition in INTEGRATION.md. Include all regions: Connection Management, Audio Controls, 
Frequency/Tuning Controls, Preset/Memory Channels, Audio Mode Controls, Signal Information, 
Power Controls, Recording Controls, and Events.
```

**Prompt 2.2: Create the EventArgs classes**
```
@workspace Create all four EventArgs classes in csharp/src/RadioProtocol.RTest.Shim/EventArgs/:
- RadioConnectionChangedEventArgs.cs
- RadioStatusChangedEventArgs.cs  
- VolumeChangedEventArgs.cs
- FrequencyChangedEventArgs.cs

Each should be a public class inheriting from EventArgs with init-only properties.
```

**Prompt 2.3: Create the RadioControlsAdapter implementation**
```
@workspace Create the RadioControlsAdapter class in 
csharp/src/RadioProtocol.RTest.Shim/Adapters/RadioControlsAdapter.cs that:
1. Implements IRadioControls
2. Takes IRadioManager and IRadioLogger in constructor
3. Subscribes to IRadioManager events and bridges them to IRadioControls events
4. Implements all interface methods by delegating to IRadioManager
5. Maintains thread-safe cached state for all properties
6. Implements IDisposable with proper cleanup
```

**Prompt 2.4: Verify adapter compilation**
```
@terminal Build the shim project and fix any compilation errors:
cd csharp && dotnet build src/RadioProtocol.RTest.Shim/RadioProtocol.RTest.Shim.csproj
```

**Verification Checklist for Phase 2:**
- [ ] `IRadioControls.cs` interface compiles with all method signatures
- [ ] All four EventArgs classes are created and compile
- [ ] `RadioControlsAdapter.cs` implements all interface members
- [ ] No compilation errors in the shim project
- [ ] Constructor properly validates arguments and subscribes to events

### Phase 3: Map RadioProtocol.Core Functionality to IRadioControls

#### Connection Management
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `ConnectAsync()` | `IRadioManager.ConnectAsync()` |
| `DisconnectAsync()` | `IRadioManager.DisconnectAsync()` |
| `IsConnected` | `IRadioManager.IsConnected` |

#### Volume Controls
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `Volume` | Cached from `RadioStatus.VolumeLevel` |
| `SetVolumeAsync()` | Multiple `AdjustVolumeAsync()` calls |
| `VolumeUpAsync()` | `AdjustVolumeAsync(up: true)` |
| `VolumeDownAsync()` | `AdjustVolumeAsync(up: false)` |
| `ToggleMuteAsync()` | `PressButtonAsync(ButtonType.Music)` or similar |

#### Frequency/Tuning Controls
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `CurrentFrequency` | Parsed from `RadioStatus.Frequency` |
| `SetFrequencyAsync()` | `PressNumberAsync()` sequence + `PressButtonAsync(Frequency)` |
| `TuneUpAsync()` | `NavigateAsync(up: true)` |
| `TuneDownAsync()` | `NavigateAsync(up: false)` |
| `ChangeBandAsync()` | `PressButtonAsync(ButtonType.Band)` |

#### Preset/Memory Channels
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `RecallPresetAsync()` | `PressNumberAsync(number, longPress: false)` |
| `SavePresetAsync()` | `PressNumberAsync(number, longPress: true)` |

#### Audio Mode Controls
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `CycleDemodulationAsync()` | `PressButtonAsync(ButtonType.Demodulation)` |
| `CycleBandwidthAsync()` | `PressButtonAsync(ButtonType.Bandwidth)` |
| `ToggleStereoAsync()` | `PressButtonAsync(ButtonType.Stereo)` |
| `CycleEqualizerAsync()` | `PressButtonAsync(ButtonType.Music)` (long press) |

#### Power Controls
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `TogglePowerAsync()` | `PressButtonAsync(ButtonType.Power)` |
| `PowerOffAsync()` | `PressButtonAsync(ButtonType.PowerLong)` |

#### Recording Controls
| IRadioControls Method | RadioProtocol.Core Implementation |
|----------------------|-----------------------------------|
| `ToggleRecordingAsync()` | `PressButtonAsync(ButtonType.Record)` |

#### GitHub Copilot Prompts for Phase 3

**Prompt 3.1: Implement connection methods**
```
@workspace In RadioControlsAdapter.cs, implement the connection management methods:
- ConnectAsync should delegate to _radioManager.ConnectAsync and return the result
- DisconnectAsync should delegate to _radioManager.DisconnectAsync  
- IsConnected property should return _radioManager.IsConnected

Ensure proper error handling and logging.
```

**Prompt 3.2: Implement volume control methods**
```
@workspace In RadioControlsAdapter.cs, implement the volume control methods:
- Volume property returns scaled value (radio 0-15 to interface 0-100)
- SetVolumeAsync calculates steps needed and calls AdjustVolumeAsync in a loop
- VolumeUpAsync and VolumeDownAsync delegate to AdjustVolumeAsync
- ToggleMuteAsync tracks mute state and sets volume to 0 or restores previous

Use the scaling formulas:
- volumePercent = (int)Math.Round((radioVolume / 15.0) * 100)
- radioVolume = (int)Math.Round((volumePercent / 100.0) * 15)
```

**Prompt 3.3: Implement frequency/tuning methods**
```
@workspace In RadioControlsAdapter.cs, implement the frequency control methods:
- CurrentFrequency returns cached frequency value
- CurrentFrequencyDisplay returns formatted string based on band
- TuneUpAsync calls NavigateAsync(up: true)
- TuneDownAsync calls NavigateAsync(up: false)  
- ChangeBandAsync calls PressButtonAsync(ButtonType.Band)
- SetFrequencyAsync uses digit-by-digit entry via PressNumberAsync

Use FormatFrequencyForEntry helper method for proper formatting per band.
```

**Prompt 3.4: Implement remaining control methods**
```
@workspace In RadioControlsAdapter.cs, implement:
- Preset methods: RecallPresetAsync, SavePresetAsync
- Audio mode methods: CycleDemodulationAsync, CycleBandwidthAsync, ToggleStereoAsync, CycleEqualizerAsync
- Power methods: TogglePowerAsync, PowerOffAsync
- Recording methods: ToggleRecordingAsync

Each should delegate to the appropriate IRadioManager method with proper error handling.
```

**Verification Checklist for Phase 3:**
- [ ] All connection methods implemented and tested
- [ ] Volume scaling works correctly (0-15 ↔ 0-100)
- [ ] Frequency entry produces correct button sequence
- [ ] All button-based methods delegate to correct ButtonType
- [ ] Error handling returns meaningful messages

### Phase 4: Event Bridging

```csharp
private void OnConnectionStateChanged(object? sender, ConnectionInfo info)
{
    ConnectionChanged?.Invoke(this, new RadioConnectionChangedEventArgs
    {
        IsConnected = info.State == ConnectionState.Connected || info.State == ConnectionState.Ready,
        DeviceAddress = info.DeviceAddress,
        ErrorMessage = info.ErrorMessage
    });
}

private void OnStatusUpdated(object? sender, RadioStatus status)
{
    // Update cached state
    _volume = status.VolumeLevel;
    _currentBand = status.Band ?? "Unknown";
    _demodulationMode = status.Demodulation ?? "Unknown";
    _isStereo = status.IsStereo;
    _isPoweredOn = status.IsPowerOn;
    
    // Parse frequency
    if (double.TryParse(status.Frequency, out var freq))
    {
        var oldFreq = _currentFrequency;
        _currentFrequency = freq;
        
        if (Math.Abs(oldFreq - freq) > 0.001)
        {
            FrequencyChanged?.Invoke(this, new FrequencyChangedEventArgs
            {
                OldFrequencyHz = oldFreq,
                NewFrequencyHz = freq,
                Band = _currentBand
            });
        }
    }
    
    StatusChanged?.Invoke(this, new RadioStatusChangedEventArgs
    {
        Band = _currentBand,
        FrequencyHz = _currentFrequency,
        Volume = _volume,
        SignalStrength = _signalStrength,
        IsStereo = _isStereo,
        DemodulationMode = _demodulationMode
    });
}
```

#### GitHub Copilot Prompts for Phase 4

**Prompt 4.1: Implement event handlers**
```
@workspace In RadioControlsAdapter.cs, implement the event handler methods:
- OnConnectionStateChanged: Bridge ConnectionInfo to RadioConnectionChangedEventArgs
- OnStatusUpdated: Update all cached state fields and raise StatusChanged event
- Track volume changes and raise VolumeChanged when volume differs
- Track frequency changes and raise FrequencyChanged when frequency differs

Ensure thread-safe updates to cached state using a lock object.
```

**Prompt 4.2: Implement IDisposable pattern**
```
@workspace In RadioControlsAdapter.cs, implement the IDisposable pattern:
- Add _disposed field to track disposal state
- Implement Dispose() method that:
  1. Unsubscribes from all IRadioManager events
  2. Sets _disposed = true
  3. Does NOT dispose the IRadioManager (it may be shared)
- Add ThrowIfDisposed() helper for use in public methods
```

**Prompt 4.3: Add disposal checks to all methods**
```
@workspace In RadioControlsAdapter.cs, add ThrowIfDisposed() calls at the start of:
- All async methods (ConnectAsync, DisconnectAsync, VolumeUpAsync, etc.)
- All property getters that access managed resources

Pattern:
private void ThrowIfDisposed()
{
    if (_disposed) throw new ObjectDisposedException(nameof(RadioControlsAdapter));
}
```

**Verification Checklist for Phase 4:**
- [ ] Events properly bridge from IRadioManager to IRadioControls events
- [ ] Cached state updates are thread-safe
- [ ] VolumeChanged fires only when volume actually changes
- [ ] FrequencyChanged fires only when frequency actually changes
- [ ] Dispose properly unsubscribes from events
- [ ] All public methods check for disposal state

### Phase 5: Frequency Entry Helper

```csharp
/// <summary>
/// Helper to enter a frequency using digit-by-digit input
/// </summary>
public async Task SetFrequencyAsync(double frequencyHz)
{
    // Convert frequency to display format based on current band
    var frequencyStr = FormatFrequencyForEntry(frequencyHz, _currentBand);
    
    _logger.LogInfo($"Entering frequency: {frequencyStr}");
    
    foreach (char c in frequencyStr)
    {
        if (char.IsDigit(c))
        {
            await _radioManager.PressNumberAsync(c - '0');
            await Task.Delay(50); // Brief delay between presses
        }
        else if (c == '.')
        {
            await _radioManager.PressButtonAsync(ButtonType.Point);
            await Task.Delay(50);
        }
    }
    
    // Confirm frequency entry
    await _radioManager.PressButtonAsync(ButtonType.Frequency);
}

private string FormatFrequencyForEntry(double frequencyHz, string band)
{
    return band.ToUpperInvariant() switch
    {
        "FM" => (frequencyHz / 1_000_000).ToString("F2"),  // MHz with 2 decimals
        "MW" => (frequencyHz / 1000).ToString("F0"),       // KHz, no decimals
        _ => (frequencyHz / 1_000_000).ToString("F3")      // MHz with 3 decimals
    };
}
```

#### GitHub Copilot Prompts for Phase 5

**Prompt 5.1: Create FrequencyConverter utility**
```
@workspace Create csharp/src/RadioProtocol.RTest.Shim/Utilities/FrequencyConverter.cs with:
- Static method FormatFrequencyForEntry(double frequencyHz, string band) -> string
- Static method ParseFrequencyDisplay(string display, string band) -> double
- Band-specific formatting (FM: 2 decimals MHz, MW: 0 decimals KHz, others: 3 decimals MHz)
- Unit tests in the test project for edge cases
```

**Prompt 5.2: Create RadioControlException**
```
@workspace Create csharp/src/RadioProtocol.RTest.Shim/Exceptions/RadioControlException.cs:
- Inherit from Exception
- Add ErrorCode property
- Add InnerErrorMessage property for RadioProtocol.Core error details
- Implement standard exception constructors
```

**Prompt 5.3: Add logging to SetFrequencyAsync**
```
@workspace In RadioControlsAdapter.SetFrequencyAsync, add comprehensive logging:
- Log the target frequency and formatted string before entry
- Log each digit/button press during entry
- Log success or failure after completion
- Handle and log any exceptions from PressNumberAsync or PressButtonAsync
```

**Verification Checklist for Phase 5:**
- [ ] FrequencyConverter produces correct formats for FM, MW, and other bands
- [ ] SetFrequencyAsync sends correct button sequence for "102.30"
- [ ] SetFrequencyAsync sends correct sequence for MW frequency "1270"
- [ ] RadioControlException captures inner error details
- [ ] Logging provides clear trace of frequency entry process

---

## Project Configuration

### RadioProtocol.RTest.Shim.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>RadioProtocol.RTest.Shim</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Shim adapter for integrating RadioProtocol.Core with RTest IRadioControls interface</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\RadioProtocol.Core\RadioProtocol.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## Usage Example

### Basic Usage

```csharp
using RadioProtocol.Core;
using RadioProtocol.Core.Logging;
using RadioProtocol.RTest.Shim.Adapters;

// Create the underlying RadioManager
var logger = new RadioLogger(/* ILogger<RadioLogger> */);
using var radioManager = new RadioManagerBuilder()
    .WithLogger(logger)
    .Build();

// Create the IRadioControls adapter
using IRadioControls radioControls = new RadioControlsAdapter(radioManager, logger);

// Subscribe to events
radioControls.StatusChanged += (s, e) => 
    Console.WriteLine($"Status: {e.Band} {e.FrequencyHz / 1_000_000} MHz, Vol: {e.Volume}");

// Connect and control
await radioControls.ConnectAsync("AA:BB:CC:DD:EE:FF");

// Control operations
await radioControls.VolumeUpAsync();
await radioControls.ChangeBandAsync();
await radioControls.SetFrequencyAsync(146_520_000); // 146.52 MHz
await radioControls.RecallPresetAsync(1);

// Disconnect
await radioControls.DisconnectAsync();
```

### Dependency Injection Setup

```csharp
services.AddSingleton<IRadioLogger, RadioLogger>();
services.AddSingleton<IRadioManager>(sp => 
{
    var logger = sp.GetRequiredService<IRadioLogger>();
    return new RadioManagerBuilder()
        .WithLogger(logger)
        .Build();
});
services.AddSingleton<IRadioControls, RadioControlsAdapter>();
```

---

## Testing Strategy

### Unit Tests

#### GitHub Copilot Prompts for Test Creation

**Prompt T1: Create test project structure**
```
@workspace Create the test file structure in csharp/tests/RadioProtocol.RTest.Shim.Tests/:
- Adapters/RadioControlsAdapterTests.cs
- Utilities/FrequencyConverterTests.cs
- Mocks/MockRadioManager.cs
- Mocks/MockRadioLogger.cs

Add package references: xunit, xunit.runner.visualstudio, Moq, FluentAssertions, coverlet.collector
```

**Prompt T2: Create MockRadioManager**
```
@workspace Create MockRadioManager.cs in csharp/tests/RadioProtocol.RTest.Shim.Tests/Mocks/ that:
- Implements IRadioManager interface
- Allows setting IsConnected, CurrentStatus, etc. via properties
- Records all method calls for verification
- Allows triggering events (ConnectionStateChanged, StatusUpdated) for testing
- Includes helper methods: SimulateConnect(), SimulateStatusUpdate(), etc.
```

### Test Coverage Requirements

#### 1. Adapter Initialization Tests

```csharp
[Fact]
public void Constructor_WithValidArguments_SubscribesToEvents()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    
    // Act
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Assert
    mockManager.VerifyAdd(m => m.ConnectionStateChanged += It.IsAny<EventHandler<ConnectionInfo>>(), Times.Once);
    mockManager.VerifyAdd(m => m.StatusUpdated += It.IsAny<EventHandler<RadioStatus>>(), Times.Once);
}

[Fact]
public void Constructor_WithNullRadioManager_ThrowsArgumentNullException()
{
    // Arrange
    var mockLogger = new Mock<IRadioLogger>();
    
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new RadioControlsAdapter(null!, mockLogger.Object));
}

[Fact]
public void Constructor_WithNullLogger_ThrowsArgumentNullException()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new RadioControlsAdapter(mockManager.Object, null!));
}
```

**Prompt T3: Generate initialization tests**
```
@workspace Create RadioControlsAdapterTests.cs with initialization tests:
- Constructor_WithValidArguments_SubscribesToEvents
- Constructor_WithNullRadioManager_ThrowsArgumentNullException
- Constructor_WithNullLogger_ThrowsArgumentNullException
- Dispose_UnsubscribesFromEvents
- DisposedAdapter_ThrowsObjectDisposedException
```

#### 2. Method Delegation Tests

```csharp
[Fact]
public async Task ConnectAsync_DelegatesToRadioManager()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    mockManager.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Act
    var result = await adapter.ConnectAsync("AA:BB:CC:DD:EE:FF");
    
    // Assert
    result.Should().BeTrue();
    mockManager.Verify(m => m.ConnectAsync("AA:BB:CC:DD:EE:FF", It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task VolumeUpAsync_CallsAdjustVolumeWithUpTrue()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    mockManager.Setup(m => m.AdjustVolumeAsync(true, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new CommandResult { Success = true });
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Act
    await adapter.VolumeUpAsync();
    
    // Assert
    mockManager.Verify(m => m.AdjustVolumeAsync(true, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task VolumeDownAsync_CallsAdjustVolumeWithUpFalse()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    mockManager.Setup(m => m.AdjustVolumeAsync(false, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new CommandResult { Success = true });
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Act
    await adapter.VolumeDownAsync();
    
    // Assert
    mockManager.Verify(m => m.AdjustVolumeAsync(false, It.IsAny<CancellationToken>()), Times.Once);
}
```

**Prompt T4: Generate delegation tests**
```
@workspace Create tests in RadioControlsAdapterTests.cs for method delegation:
- ConnectAsync_DelegatesToRadioManager
- DisconnectAsync_DelegatesToRadioManager
- VolumeUpAsync_CallsAdjustVolumeWithUpTrue
- VolumeDownAsync_CallsAdjustVolumeWithUpFalse
- TuneUpAsync_CallsNavigateWithUpTrue
- TuneDownAsync_CallsNavigateWithUpFalse
- ChangeBandAsync_CallsPressButtonWithBandType
- RecallPresetAsync_CallsPressNumberWithoutLongPress
- SavePresetAsync_CallsPressNumberWithLongPress
- TogglePowerAsync_CallsPressButtonWithPowerType
- PowerOffAsync_CallsPressButtonWithPowerLongType
```

#### 3. Volume Scaling Tests

> **Rounding Method**: Uses `Math.Round()` with default MidpointRounding.ToEven (banker's rounding).
> For consistent results, consider using `Math.Round(value, MidpointRounding.AwayFromZero)`.

```csharp
[Theory]
[InlineData(0, 0)]     // 0/15 * 100 = 0
[InlineData(15, 100)]  // 15/15 * 100 = 100
[InlineData(7, 47)]    // 7/15 * 100 = 46.67 → rounds to 47
[InlineData(8, 53)]    // 8/15 * 100 = 53.33 → rounds to 53
public void Volume_ReturnsScaledValue(int radioVolume, int expectedPercent)
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Simulate status update with radio volume
    var status = new RadioStatus { VolumeLevel = radioVolume };
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, status);
    
    // Act & Assert
    adapter.Volume.Should().Be(expectedPercent);
}

[Theory]
[InlineData(0, 0)]     // 0/100 * 15 = 0
[InlineData(100, 15)]  // 100/100 * 15 = 15
[InlineData(50, 8)]    // 50/100 * 15 = 7.5 → rounds to 8 (MidpointRounding.AwayFromZero)
[InlineData(33, 5)]    // 33/100 * 15 = 4.95 → rounds to 5
public async Task SetVolumeAsync_CalculatesCorrectSteps(int targetPercent, int expectedRadioLevel)
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    mockManager.Setup(m => m.AdjustVolumeAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new CommandResult { Success = true });
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Set initial state to 0
    var status = new RadioStatus { VolumeLevel = 0 };
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, status);
    
    // Act
    await adapter.SetVolumeAsync(targetPercent);
    
    // Assert - should call AdjustVolumeAsync(up: true) expectedRadioLevel times
    mockManager.Verify(m => m.AdjustVolumeAsync(true, It.IsAny<CancellationToken>()), 
                       Times.Exactly(expectedRadioLevel));
}
```

**Prompt T5: Generate volume scaling tests**
```
@workspace Create volume scaling tests in RadioControlsAdapterTests.cs:
- Volume_ReturnsScaledValue with Theory for 0, 7, 8, 15 radio values
- SetVolumeAsync_CalculatesCorrectSteps for 0, 50, 100 percent targets
- SetVolumeAsync_DecrementsWhenCurrentHigher
- ToggleMuteAsync_SetsVolumeToZero_WhenNotMuted
- ToggleMuteAsync_RestoresVolume_WhenMuted
```

#### 4. State Management Tests

```csharp
[Fact]
public void StatusUpdated_UpdatesCachedState()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    var status = new RadioStatus
    {
        VolumeLevel = 10,
        Band = "FM",
        Demodulation = "STEREO",
        IsStereo = true,
        IsPowerOn = true,
        Frequency = "102300000"
    };
    
    // Act
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, status);
    
    // Assert
    adapter.Volume.Should().Be(67); // 10/15 * 100 ≈ 67
    adapter.CurrentBand.Should().Be("FM");
    adapter.DemodulationMode.Should().Be("STEREO");
    adapter.IsStereo.Should().BeTrue();
    adapter.IsPoweredOn.Should().BeTrue();
    adapter.CurrentFrequency.Should().Be(102300000);
}

[Fact]
public void IsConnected_DelegatesToRadioManager()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    mockManager.Setup(m => m.IsConnected).Returns(true);
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Act & Assert
    adapter.IsConnected.Should().BeTrue();
}
```

**Prompt T6: Generate state management tests**
```
@workspace Create state management tests in RadioControlsAdapterTests.cs:
- StatusUpdated_UpdatesCachedState for all properties
- StatusUpdated_ThreadSafe when called from multiple threads
- IsConnected_DelegatesToRadioManager
- CurrentStatus_ReflectsLatestUpdate
```

#### 5. Event Bridge Tests

```csharp
[Fact]
public void ConnectionStateChanged_RaisesConnectionChanged()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    RadioConnectionChangedEventArgs? receivedArgs = null;
    adapter.ConnectionChanged += (s, e) => receivedArgs = e;
    
    var connectionInfo = new ConnectionInfo(ConnectionState.Connected, "AA:BB:CC:DD:EE:FF", DateTime.Now, null);
    
    // Act
    mockManager.Raise(m => m.ConnectionStateChanged += null, mockManager.Object, connectionInfo);
    
    // Assert
    receivedArgs.Should().NotBeNull();
    receivedArgs!.IsConnected.Should().BeTrue();
    receivedArgs.DeviceAddress.Should().Be("AA:BB:CC:DD:EE:FF");
}

[Fact]
public void StatusUpdated_RaisesStatusChanged()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    RadioStatusChangedEventArgs? receivedArgs = null;
    adapter.StatusChanged += (s, e) => receivedArgs = e;
    
    var status = new RadioStatus { VolumeLevel = 10, Band = "FM" };
    
    // Act
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, status);
    
    // Assert
    receivedArgs.Should().NotBeNull();
    receivedArgs!.Volume.Should().Be(67); // Scaled
    receivedArgs.Band.Should().Be("FM");
}

[Fact]
public void FrequencyChange_RaisesFrequencyChanged()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    FrequencyChangedEventArgs? receivedArgs = null;
    adapter.FrequencyChanged += (s, e) => receivedArgs = e;
    
    // First status to set initial frequency
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, 
                      new RadioStatus { Frequency = "102300000" });
    receivedArgs = null; // Reset
    
    // Act - Update to new frequency
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, 
                      new RadioStatus { Frequency = "103500000", Band = "FM" });
    
    // Assert
    receivedArgs.Should().NotBeNull();
    receivedArgs!.OldFrequencyHz.Should().Be(102300000);
    receivedArgs.NewFrequencyHz.Should().Be(103500000);
    receivedArgs.Band.Should().Be("FM");
}

[Fact]
public void SameFrequency_DoesNotRaiseFrequencyChanged()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    int eventCount = 0;
    adapter.FrequencyChanged += (s, e) => eventCount++;
    
    // Act - Send same frequency twice
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, 
                      new RadioStatus { Frequency = "102300000" });
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, 
                      new RadioStatus { Frequency = "102300000" });
    
    // Assert - Should only fire once (first time)
    eventCount.Should().Be(1);
}
```

**Prompt T7: Generate event bridge tests**
```
@workspace Create event bridge tests in RadioControlsAdapterTests.cs:
- ConnectionStateChanged_RaisesConnectionChanged
- StatusUpdated_RaisesStatusChanged
- FrequencyChange_RaisesFrequencyChanged
- SameFrequency_DoesNotRaiseFrequencyChanged
- VolumeChange_RaisesVolumeChanged
- SameVolume_DoesNotRaiseVolumeChanged
- Dispose_StopsRaisingEvents
```

#### 6. Frequency Converter Tests

```csharp
[Theory]
[InlineData(102300000, "FM", "102.30")]
[InlineData(88100000, "FM", "88.10")]
[InlineData(1270000, "MW", "1270")]
[InlineData(530000, "MW", "530")]
[InlineData(146520000, "VHF", "146.520")]
[InlineData(119100000, "AIR", "119.100")]
public void FormatFrequencyForEntry_FormatsCorrectly(double frequencyHz, string band, string expected)
{
    // Act
    var result = FrequencyConverter.FormatFrequencyForEntry(frequencyHz, band);
    
    // Assert
    result.Should().Be(expected);
}

[Theory]
[InlineData("102.30", "FM", 102300000)]
[InlineData("1270", "MW", 1270000)]
[InlineData("146.520", "VHF", 146520000)]
public void ParseFrequencyDisplay_ParsesCorrectly(string display, string band, double expectedHz)
{
    // Act
    var result = FrequencyConverter.ParseFrequencyDisplay(display, band);
    
    // Assert
    result.Should().BeApproximately(expectedHz, 1);
}
```

**Prompt T8: Generate frequency converter tests**
```
@workspace Create FrequencyConverterTests.cs with:
- FormatFrequencyForEntry tests for FM, MW, VHF, AIR bands
- ParseFrequencyDisplay tests for reverse conversion
- Edge cases: very low frequencies, very high frequencies
- Invalid input handling (null, empty, invalid format)
```

### Integration Tests

```csharp
[Fact]
public async Task FullConnectionLifecycle_WorksCorrectly()
{
    // Arrange
    var mockManager = new MockRadioManager();
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager, mockLogger.Object);
    
    var connectionEvents = new List<bool>();
    adapter.ConnectionChanged += (s, e) => connectionEvents.Add(e.IsConnected);
    
    // Act - Connect
    mockManager.SimulateConnectSuccess();
    var connectResult = await adapter.ConnectAsync("AA:BB:CC:DD:EE:FF");
    
    // Assert - Connected
    connectResult.Should().BeTrue();
    adapter.IsConnected.Should().BeTrue();
    connectionEvents.Should().Contain(true);
    
    // Act - Disconnect
    await adapter.DisconnectAsync();
    mockManager.SimulateDisconnect();
    
    // Assert - Disconnected
    adapter.IsConnected.Should().BeFalse();
    connectionEvents.Should().Contain(false);
}

[Fact]
public async Task SetFrequency_SendsCorrectButtonSequence()
{
    // Arrange
    var mockManager = new Mock<IRadioManager>();
    var buttonPresses = new List<(int? number, ButtonType? button, bool longPress)>();
    
    mockManager.Setup(m => m.PressNumberAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .Callback<int, bool, CancellationToken>((n, lp, ct) => buttonPresses.Add((n, null, lp)))
               .ReturnsAsync(new CommandResult { Success = true });
    
    mockManager.Setup(m => m.PressButtonAsync(It.IsAny<ButtonType>(), It.IsAny<CancellationToken>()))
               .Callback<ButtonType, CancellationToken>((bt, ct) => buttonPresses.Add((null, bt, false)))
               .ReturnsAsync(new CommandResult { Success = true });
    
    var mockLogger = new Mock<IRadioLogger>();
    using var adapter = new RadioControlsAdapter(mockManager.Object, mockLogger.Object);
    
    // Set band to FM
    mockManager.Raise(m => m.StatusUpdated += null, mockManager.Object, new RadioStatus { Band = "FM" });
    
    // Act - Enter 102.30 MHz
    await adapter.SetFrequencyAsync(102300000);
    
    // Assert - Should press: 1, 0, 2, ., 3, 0, Frequency
    buttonPresses.Should().HaveCount(7);
    buttonPresses[0].number.Should().Be(1);
    buttonPresses[1].number.Should().Be(0);
    buttonPresses[2].number.Should().Be(2);
    buttonPresses[3].button.Should().Be(ButtonType.Point);
    buttonPresses[4].number.Should().Be(3);
    buttonPresses[5].number.Should().Be(0);
    buttonPresses[6].button.Should().Be(ButtonType.Frequency);
}
```

**Prompt T9: Generate integration tests**
```
@workspace Create integration tests in RadioControlsAdapterTests.cs:
- FullConnectionLifecycle_WorksCorrectly
- SetFrequency_SendsCorrectButtonSequence for FM and MW bands
- PresetSaveRecall_WorksCorrectly
- ErrorHandling_PropagatesExceptions
```

### Running Tests

**Prompt T10: Run and verify all tests**
```
@terminal Run the test suite and verify coverage:
cd csharp
dotnet test tests/RadioProtocol.RTest.Shim.Tests --collect:"XPlat Code Coverage"

# Install reportgenerator if not already installed:
# dotnet tool install -g dotnet-reportgenerator-globaltool

dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Test Coverage Targets

| Component | Target Coverage |
|-----------|-----------------|
| RadioControlsAdapter | 90%+ |
| FrequencyConverter | 100% |
| EventArgs classes | 100% |
| RadioControlException | 100% |

**Verification Checklist for Tests:**
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Code coverage meets targets
- [ ] No flaky tests
- [ ] Tests run in CI/CD pipeline

---

## Limitations and Considerations

### Known Limitations

1. **Volume Level Granularity**: The radio uses 0-15 volume levels; the interface exposes 0-100. A scaling factor will be applied:
   ```csharp
   // Convert from radio (0-15) to interface (0-100)
   int volumePercent = (int)Math.Round((radioVolume / 15.0) * 100);
   
   // Convert from interface (0-100) to radio (0-15)
   int radioVolume = (int)Math.Round((volumePercent / 100.0) * 15);
   ```

2. **Mute Implementation**: The radio may not have a dedicated mute command. Implementation may use volume=0 or a specific button.

3. **Frequency Entry**: Direct frequency setting requires digit-by-digit entry and confirmation, which takes time.

4. **Async Nature**: All operations are asynchronous; callers should handle timing appropriately.

5. **State Synchronization**: Cached state may be briefly out of sync after commands until status updates arrive.

### Thread Safety

The adapter maintains internal state that should be thread-safe:

```csharp
private readonly object _stateLock = new();

public int Volume
{
    get { lock (_stateLock) return _volume; }
    private set { lock (_stateLock) _volume = value; }
}
```

### Error Handling

```csharp
public async Task VolumeUpAsync()
{
    var result = await _radioManager.AdjustVolumeAsync(up: true);
    if (!result.Success)
    {
        _logger.LogError(null, $"Volume up failed: {result.ErrorMessage}");
        throw new RadioControlException("Failed to increase volume", result.ErrorMessage);
    }
}
```

---

## Migration Path

### Step 1: Create Interface Project
Create `RTest.Radio.Core.Interfaces` project with `IRadioControls` and event args.

**GitHub Copilot Prompt:**
```
@workspace Execute Phase 1 prompts 1.1-1.3 to create the project structure and test project.
Verify with: dotnet build csharp/RadioProtocol.sln
```

### Step 2: Create Shim Project
Create `RadioProtocol.RTest.Shim` with `RadioControlsAdapter` implementation.

**GitHub Copilot Prompt:**
```
@workspace Execute Phase 2 prompts 2.1-2.4 to create the IRadioControls interface, 
EventArgs classes, and RadioControlsAdapter implementation.
Verify with: dotnet build csharp/src/RadioProtocol.RTest.Shim
```

### Step 3: Implement All Methods
Complete the method implementations and event bridging.

**GitHub Copilot Prompt:**
```
@workspace Execute Phase 3 prompts 3.1-3.4 and Phase 4 prompts 4.1-4.3 to implement 
all adapter methods, event handlers, and IDisposable pattern.
Verify with: dotnet build csharp/src/RadioProtocol.RTest.Shim
```

### Step 4: Add Utilities and Exception Handling
Create helper utilities and custom exceptions.

**GitHub Copilot Prompt:**
```
@workspace Execute Phase 5 prompts 5.1-5.3 to create FrequencyConverter, 
RadioControlException, and add comprehensive logging.
Verify with: dotnet build csharp/src/RadioProtocol.RTest.Shim
```

### Step 5: Create and Run Tests
Implement comprehensive test coverage.

**GitHub Copilot Prompt:**
```
@workspace Execute Test prompts T1-T10 to create all test classes and run them.
Verify with: dotnet test csharp/tests/RadioProtocol.RTest.Shim.Tests --verbosity normal
```

### Step 6: Update RTest Project
Reference the shim project and configure DI in RTest.

**GitHub Copilot Prompt:**
```
@workspace In the RTest project:
1. Add a project reference to RadioProtocol.RTest.Shim
2. Configure dependency injection for IRadioControls
3. Replace any direct RadioProtocol.Core usage with IRadioControls
```

### Step 7: Final Verification
Run all tests and verify the complete integration.

**GitHub Copilot Prompt:**
```
@terminal Run the complete verification:
cd csharp
dotnet build RadioProtocol.sln
dotnet test --collect:"XPlat Code Coverage"
echo "Verify all tests pass and coverage targets are met"
```

---

## File Checklist

When implementing this plan, create or modify these files:

**In RTest project (paths relative to RTest root):**
- [ ] `src/Radio.Core/Interfaces/Audio/IRadioControls.cs`
- [ ] `src/Radio.Core/Interfaces/Audio/EventArgs/RadioConnectionChangedEventArgs.cs`
- [ ] `src/Radio.Core/Interfaces/Audio/EventArgs/RadioStatusChangedEventArgs.cs`
- [ ] `src/Radio.Core/Interfaces/Audio/EventArgs/VolumeChangedEventArgs.cs`
- [ ] `src/Radio.Core/Interfaces/Audio/EventArgs/FrequencyChangedEventArgs.cs`

**In RadioProtocol repository (paths relative to csharp/ folder):**
- [ ] `src/RadioProtocol.RTest.Shim/RadioProtocol.RTest.Shim.csproj`
- [ ] `src/RadioProtocol.RTest.Shim/Adapters/RadioControlsAdapter.cs`
- [ ] `src/RadioProtocol.RTest.Shim/Extensions/RadioManagerExtensions.cs`
- [ ] `src/RadioProtocol.RTest.Shim/Utilities/FrequencyConverter.cs`
- [ ] `src/RadioProtocol.RTest.Shim/Exceptions/RadioControlException.cs`
- [ ] `tests/RadioProtocol.RTest.Shim.Tests/RadioControlsAdapterTests.cs`
- [ ] Update `RadioProtocol.sln` to include new projects

---

## Summary

This integration plan provides a clean adapter pattern to bridge `RadioProtocol.Core` functionality to an `IRadioControls` interface suitable for the `RTest` project. The shim:

1. **Encapsulates** all `RadioProtocol.Core` complexity behind a simple interface
2. **Translates** events from the underlying library to a consistent event model
3. **Provides** helper methods for common operations like frequency entry
4. **Maintains** internal state synchronized with the radio
5. **Follows** .NET best practices for async programming and DI

The implementation is designed to be:
- **Testable** through interface-based design
- **Extensible** for additional functionality
- **Thread-safe** for concurrent usage
- **Well-documented** for maintainability
