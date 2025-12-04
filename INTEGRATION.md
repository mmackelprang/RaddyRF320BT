# RTest Integration Plan: RadioControl.Core to IRadioControls Shim

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
│   └── RadioManagerExtensions.cs   # Helper extension methods
└── Utilities/
    └── FrequencyConverter.cs       # Frequency format utilities
```

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

1. **Adapter Initialization Tests**
   - Verify proper subscription to RadioManager events
   - Test null argument handling

2. **Method Delegation Tests**
   - Mock `IRadioManager` and verify correct method calls
   - Test command translation (e.g., `VolumeUpAsync` calls `AdjustVolumeAsync(true)`)

3. **State Management Tests**
   - Verify cached properties update on status events
   - Test thread safety of state updates

4. **Event Bridge Tests**
   - Verify events are properly translated and raised
   - Test event args population

### Integration Tests

1. **Connection Lifecycle**
   - Connect/disconnect scenarios
   - Reconnection handling

2. **End-to-End Control Flow**
   - Full frequency entry sequence
   - Preset save/recall cycles

---

## Limitations and Considerations

### Known Limitations

1. **Volume Level Granularity**: The radio uses 0-15 volume levels; the interface exposes 0-100. A scaling factor will be applied.

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

### Step 2: Create Shim Project
Create `RadioProtocol.RTest.Shim` with `RadioControlsAdapter` implementation.

### Step 3: Update RTest Project
Reference the shim project and configure DI.

### Step 4: Replace Direct Dependencies
Replace any direct `RadioProtocol.Core` usage in `RTest` with `IRadioControls`.

### Step 5: Testing
Run comprehensive tests to verify functionality.

---

## File Checklist

When implementing this plan, create or modify these files:

- [ ] `RTest/src/Radio.Core/Interfaces/Audio/IRadioControls.cs`
- [ ] `RTest/src/Radio.Core/Interfaces/Audio/EventArgs/RadioConnectionChangedEventArgs.cs`
- [ ] `RTest/src/Radio.Core/Interfaces/Audio/EventArgs/RadioStatusChangedEventArgs.cs`
- [ ] `RTest/src/Radio.Core/Interfaces/Audio/EventArgs/VolumeChangedEventArgs.cs`
- [ ] `RTest/src/Radio.Core/Interfaces/Audio/EventArgs/FrequencyChangedEventArgs.cs`
- [ ] `csharp/src/RadioProtocol.RTest.Shim/RadioProtocol.RTest.Shim.csproj`
- [ ] `csharp/src/RadioProtocol.RTest.Shim/Adapters/RadioControlsAdapter.cs`
- [ ] `csharp/src/RadioProtocol.RTest.Shim/Extensions/RadioManagerExtensions.cs`
- [ ] `csharp/src/RadioProtocol.RTest.Shim/Utilities/FrequencyConverter.cs`
- [ ] `csharp/src/RadioProtocol.RTest.Shim/Exceptions/RadioControlException.cs`
- [ ] `csharp/tests/RadioProtocol.RTest.Shim.Tests/RadioControlsAdapterTests.cs`
- [ ] Update `csharp/RadioProtocol.sln` to include new projects

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
