# RadioClient Test Application

A comprehensive C# console application for testing and interacting with the RF320 BLE radio device. This project includes a complete protocol implementation with Windows BLE support, interactive keyboard control, and real-time status monitoring.

## Current Status (November 13, 2025)

### ✅ Fully Working
- **Command Transmission**: All button commands send correctly with `WriteWithResponse`
- **Band Switching**: Cycles through all bands (AIR/WB/FM/VHF/MW/SW)
- **Volume Control**: VolAdd/VolDel update radio volume in real-time
- **Frequency Entry**: Number keys + Point + FreqConfirm accepted by radio
- **Status Monitoring**: Real-time updates for volume and modulation mode
- **BLE Connection**: Stable connection using Service ff12, Characteristics ff13 (TX) and ff14 (RX)

### ⚠️ Partially Working
- **Frequency Display**: Raw frequency values captured from ab0901 messages, but decoding formula incomplete
- **Modulation Mode**: Shows AM/NFM/WFM (demodulation type) instead of band names (AIR/VHF/etc)

### ❌ Not Yet Implemented
- **Band Name Display**: AIR/WB/FM/VHF/MW/SW not available in BLE messages
- **Signal Strength**: SNR/RSSI values received but not parsed numerically
- **Battery Level**: Not found in status messages

## Features

### Protocol Implementation
- **Frame builder** with automatic checksum generation for button and ack groups
- **Handshake** initialization detecting successful connection via status stream
- **Command transmission** using `WriteWithResponse` (required for radio firmware to process commands)
- **Status message parsing** for volume, modulation mode, and fractional frequency
- **Frequency state capture** from ab0901 messages with raw value logging
- **Heartbeat monitoring** via continuous 0x1C status message stream

### Testing Application
- **Automatic BLE scanning** for RF320 devices with signal strength reporting
- **Full Windows BLE integration** using Windows.Devices.Bluetooth APIs
- **Interactive keyboard control** with mnemonic key mappings
- **Command-line automation mode** for scripted testing
- **Comprehensive message logging** with timestamps (including milliseconds)
- **Real-time status display** showing volume, modulation, and frequency updates
- **Connection monitoring** with status message heartbeat tracking

### Logging
Every BLE message is logged to a file with:
- **Timestamp** with millisecond precision (yyyy-MM-dd HH:mm:ss.fff)
- **Message Source** (Radio or Application)
- **Message Data** as hex string with byte-by-byte formatting
- **Message Type** decoded from the protocol (e.g., "Button: Number1", "Status: VolumeValue")
- **Status Updates** including volume changes, modulation mode, and raw frequency data

Log files are stored in: `%LocalAppData%\RadioClient\Logs\RadioClient_YYYYMMDD_HHmmss.log`

## Project Structure

### Core Protocol Files
- **`IRadioTransport.cs`** - Abstraction layer for BLE I/O operations
- **`RadioProtocol.cs`** - Frame and state parsing logic with protocol definitions
- **`RadioBT.cs`** - High-level radio session manager with event handling
- **`FrameFactory.cs`** - Helper methods for building protocol frames

### Application Files
- **`Program.cs`** - Main console application with BLE scanning and keyboard handling
- **`WinBleTransport.cs`** - Windows BLE implementation of IRadioTransport
- **`MessageLogger.cs`** - Thread-safe logging system for all radio communications
- **`KeyboardMapper.cs`** - Keyboard-to-CanonicalAction mapping with help display

### Configuration
- **`RadioClient.csproj`** - .NET 8.0 Windows project targeting Windows 10.0.19041.0

## Building and Running

### Requirements
- **Windows 10** version 1809 (build 17763) or later
- **.NET 8.0 SDK** or later
- **Bluetooth LE** support (built-in or USB adapter)

### Build
```powershell
Set-Location c:\prj\RaddyRF320BT\csharp\src\testClient
dotnet build
```

### Run

**Interactive Mode** (keyboard control):
```powershell
dotnet run
```

**Command-Line Mode** (automated testing):
```powershell
dotnet run -- <action1> <action2> ...
```

Or after building:
```powershell
.\bin\Debug\net8.0-windows10.0.19041.0\win-x64\RadioClient.exe
```

## Using the Test Application

### Mode Selection

The application supports two operating modes:

#### 1. Interactive Mode (default)
Run without arguments to start keyboard control:
```powershell
dotnet run
```

1. Ensure your RF320 radio is powered on and advertising
2. Run the application - it will automatically scan for devices
3. When an RF320 device is found, the app will connect automatically
4. After successful connection and handshake, the keyboard interface activates

#### 2. Command-Line Mode (automated)
Pass command names as arguments to send them automatically:
```powershell
# Test band change
dotnet run -- Band

# Test volume control
dotnet run -- VolAdd VolAdd VolDel

# Test frequency entry (123.45 MHz)
dotnet run -- Number1 Number2 Number3 Point Number4 Number5 FreqConfirm
```

**Command-Line Mode Behavior:**
- Connects to device automatically
- Sends all specified commands with 100ms spacing
- Waits 5 seconds for responses
- Exits automatically
- All messages logged to file

**Available Command Names:**
Use the CanonicalAction enum names (case-sensitive):
- Numbers: `Number0` through `Number9`
- Volume: `VolAdd`, `VolDel`
- Navigation: `FreqUp`, `FreqDown`, `FreqUpHold`, `FreqDownHold`
- Functions: `Band`, `Power`, `FreqConfirm`, `Point`, `Back`
- Extended: `SubBand`, `Music`, `Play`, `PlayHold`, `Step`, `Circle`, etc.

**Example Test Sessions:**
```powershell
# Quick band test
dotnet run -- Band

# Volume control test
dotnet run -- VolAdd VolAdd VolAdd VolDel VolDel

# Frequency entry: 146.52 MHz
dotnet run -- Number1 Number4 Number6 Point Number5 Number2 FreqConfirm

# Multiple function test
dotnet run -- Power Band VolAdd FreqUp
```

All test results are saved to log files in `%LocalAppData%\RadioClient\Logs\`

### Keyboard Controls

#### Numbers (0-9)
- **0-9 keys** or **NumPad 0-9**: Send number commands
- **Ctrl+0-9**: Send number hold commands (long press simulation)

#### Navigation
- **↑ (Up Arrow)**: Increment (short press)
- **↓ (Down Arrow)**: Decrement (short press)
- **Shift+↑**: Increment (long press)
- **Shift+↓**: Decrement (long press)

#### Volume Control
- **+ (Plus/Add)**: Volume Up
- **- (Minus/Subtract)**: Volume Down

#### Radio Functions
| Key | Function | Key | Function |
|-----|----------|-----|----------|
| **B** | Band | **M** | Music |
| **P** | Power | **L** | Play |
| **S** | Step | **C** | Circle |
| **T** | Sub-Band | **Q** | SQ (Squelch) |
| **R** | Record | **D** | Demodulation |
| **W** | BandWidth | **O** | Mobile Display |
| **E** | Stereo | **Y** | De-Emphasis |
| **X** | Preset | **N** | Memo |
| **U** | Bluetooth | | |

#### Special Keys
- **.** (Period): Decimal point
- **Enter**: Frequency confirm
- **Backspace**: Back
- **Spacebar**: Music cycle
- **Escape**: Exit application

#### Hold Functions (Ctrl+Key)
Hold commands simulate long button presses:
- **Ctrl+0-9**: Number holds
- **Ctrl+P**: Power hold
- **Ctrl+M**: Music hold
- **Ctrl+L**: Play hold
- **Ctrl+.**: Point hold
- **Ctrl+N**: Memo hold

### Screen Output
The application provides real-time feedback:
```
  → TX: D → Demodulation          (Sent to radio)
  ← RX: Ack: SUCCESS               (Received from radio)
  ← STATE: 146.52000 MHz (MHz)    (Radio state update)
```

### Exiting
Press **ESC** to exit. The application will:
1. Close the BLE connection gracefully
2. Display the location of the log file
3. Save all logged data

## Code Usage Examples

### Basic Protocol Usage
```csharp
IRadioTransport transport = /* platform implementation */;
var radio = new RadioBT(transport);

// Initialize with handshake
if (await radio.InitializeAsync())
{
    // Send commands
    await radio.SendAsync(CanonicalAction.Power);
    await radio.SendAsync(CanonicalAction.Number1);
    await radio.SendAsync(CanonicalAction.FreqConfirm);
}

// Monitor state updates
radio.StateUpdated += (_, state) => 
{
    Console.WriteLine($"Frequency: {state.FrequencyMHz:0.00000} MHz");
};

// Monitor frame reception
radio.FrameReceived += (_, frame) =>
{
    if (frame.Group == CommandGroup.Ack && frame.CommandId == 0x01)
    {
        Console.WriteLine("Command acknowledged!");
    }
};
```

### Creating Custom Transport
```csharp
public sealed class CustomTransport : IRadioTransport
{
    public event EventHandler<byte[]>? NotificationReceived;
    
    public async Task<bool> WriteAsync(byte[] data)
    {
        // Write to GATT characteristic (UUID: 0000fff2-...)
        // Return true if successful
    }
    
    public void Dispose()
    {
        // Clean up GATT connection
    }
    
    private void OnDataReceived(byte[] data)
    {
        // Call when data received from characteristic (UUID: 0000fff1-...)
        NotificationReceived?.Invoke(this, data);
    }
}
```

## Troubleshooting

### No Device Found
- Ensure RF320 radio is powered on
- Check that Bluetooth is enabled on your PC
- Verify the device name contains "RF320"
- Try moving closer to the radio

### Connection Failed
- Restart the radio
- Restart Bluetooth on your PC
- Run the application as Administrator if permission errors occur
- Check Windows Bluetooth & devices settings

### Commands Not Working
- Verify handshake completed (look for "Ack: SUCCESS" after "Initializing radio")
- Check the log file for error messages
- Ensure radio is not in a locked or special mode

### Log File Location
If you can't find the log file, it's at:
```
%LocalAppData%\RadioClient\Logs\RadioClient_YYYYMMDD_HHmmss.log
```

## Protocol Details

### Service and Characteristics (✅ Hardware Verified)
- **Vendor Service UUID**: `0000ff12-0000-1000-8000-00805f9b34fb`
- **TX Characteristic** (Write): `0000ff13-0000-1000-8000-00805f9b34fb`
- **RX Characteristic** (Notify): `0000ff14-0000-1000-8000-00805f9b34fb`

Additional services:
- **Battery Service**: `0000180f-0000-1000-8000-00805f9b34fb` (standard BLE service)
- **Alternative Service**: `0000ff10-0000-1000-8000-00805f9b34fb` (purpose unclear)

### Frame Format
```
Standard Command: [AB 02 GG CC XX]
  AB    = Header
  02    = Protocol version
  GG    = Command group (0C=Button, 12=Ack, 1C=Status)
  CC    = Command ID
  XX    = Checksum (Base + CommandID)

Handshake: [AB 01 FF AB]
  Note: Device does NOT send ACK response
  Instead: Device starts streaming status messages (Group 1C)

Status Messages: [AB LEN 1C TYPE DATA...]
  AB    = Header
  LEN   = Length indicator (05 or 06)
  1C    = Status group
  TYPE  = Subtype (06 or 08 observed)
  DATA  = Variable payload
  
  Device continuously streams status (~2-3 msg/sec)
```

## Next Steps for Complete Status Decoding

### High Priority
1. **Frequency Decoding Formula**
   - Current data points collected (see TESTING_RESULTS.md)
   - Need to determine correct conversion from raw 24-bit value + scale factor
   - Tested bands: AIR (119.345 MHz), WB (162.40 MHz), FM (102.30 MHz), VHF (145.095 MHz), MW (1.270 MHz)
   - Raw values and scale factors logged for analysis

2. **Signal Strength Parsing**
   - Type 0x05 (SNR) and 0x07 (RSSI) receive ASCII labels
   - Need to find numeric value in message payload
   - Radio displays 2-digit signal strength that fluctuates

3. **Band Name Detection**
   - Type 0x02 only shows modulation (AM/NFM/WFM)
   - Actual band (AIR/WB/FM/VHF/MW/SW) not found in current status messages
   - May require analyzing other message groups (0x19, 0x21) or ab0901 fields

### Medium Priority
4. **Additional Status Fields**
   - Type 0x01 (Demodulation), 0x03 (BandWidth), 0x04 (Unknown) send labels only
   - Type 0x0B (Model), 0x0C (Status/EQ) partially decoded
   - Type 0x10 (Recording status) shows "REC OFF" correctly
   - Byte 6-8 values in ab0901 message purpose unclear

5. **Fractional Frequency Correlation**
   - Types 0x06 and 0x08 show decimal portions (e.g., ".345" from "119.345")
   - Not always accurate or synchronized with main frequency
   - May represent secondary receiver or scan frequency

### Low Priority
6. **ab090f Frame Support**
   - Alternate state message format observed in logs
   - Layout differs from ab0901
   - Purpose and decoding unknown

7. **Battery Level**
   - Standard BLE Battery Service (0x180f) present
   - Not accessed by current implementation
   - May require separate GATT read operation

## Future Enhancements
- Implement configuration file for custom key mappings
- Add batch command scripting from files
- Support for multiple simultaneous radio connections
- Enhanced frequency scanning and monitoring modes
- Real-time frequency spectrum display
- Preset/memory channel management

## Contributing
To help decode the remaining status fields:
1. Run the app with `dotnet run` and press ESC after 10-20 seconds
2. Share the log file from `%LocalAppData%\RadioClient\Logs\`
3. Include the actual radio display readings (band, frequency, volume, signal)
4. Note any changes you made during the capture (button presses, frequency changes)

## License
This is a reverse-engineered protocol implementation for educational and testing purposes.
