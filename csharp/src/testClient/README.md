# RadioClient Test Application

A comprehensive C# console application for testing and interacting with the RF320 BLE radio device. This project includes a complete protocol implementation with Windows BLE support and interactive keyboard control.

## Features

### Protocol Implementation
- **Frame builder** with automatic checksum generation for button and ack groups
- **Handshake** initialization and success detection
- **Adaptive frequency parsing** for `ab0901` snapshot frames with intelligent scaling
- **Heartbeat monitoring** via `0x1C` status frames to maintain connection health
- **Collision-safe canonical action enum** with comprehensive command mapping

### Testing Application
- **Automatic BLE scanning** for RF320 devices with signal strength reporting
- **Full Windows BLE integration** using Windows.Devices.Bluetooth APIs
- **Interactive keyboard control** with mnemonic key mappings
- **Comprehensive message logging** with timestamps (including milliseconds)
- **Real-time feedback** showing all sent and received messages
- **Connection monitoring** with automatic reconnection attempts

### Logging
Every BLE message is logged to a file with:
- **Timestamp** with millisecond precision (yyyy-MM-dd HH:mm:ss.fff)
- **Message Source** (Radio or Application)
- **Message Data** as hex string
- **Message Type** decoded from the protocol (e.g., "Button: Number1", "Ack: Success")
- **Radio State Updates** including frequency and operating mode

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
```powershell
dotnet run
```

Or after building:
```powershell
.\bin\Debug\net8.0-windows10.0.19041.0\win-x64\RadioClient.exe
```

## Using the Test Application

### Starting the Application
1. Ensure your RF320 radio is powered on and advertising
2. Run the application - it will automatically scan for devices
3. When an RF320 device is found, the app will connect automatically
4. After successful connection and handshake, the keyboard interface activates

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

### Service and Characteristics
- **UART Service UUID**: `0000fff0-0000-1000-8000-00805f9b34fb`
- **TX Characteristic** (Write): `0000fff2-0000-1000-8000-00805f9b34fb`
- **RX Characteristic** (Notify): `0000fff1-0000-1000-8000-00805f9b34fb`

### Frame Format
```
Standard Command: [AB 02 GG CC XX]
  AB    = Header
  02    = Protocol version
  GG    = Command group (0C=Button, 12=Ack)
  CC    = Command ID
  XX    = Checksum (Base + CommandID)

Handshake: [AB 01 FF AB]
```

## Future Enhancements
- Add support for `ab090f` alternate snapshot layout
- Implement configuration file for custom key mappings
- Add batch command scripting
- Support for multiple simultaneous connections
- Enhanced frequency scanning modes

## License
This is a reverse-engineered protocol implementation for educational and testing purposes.
