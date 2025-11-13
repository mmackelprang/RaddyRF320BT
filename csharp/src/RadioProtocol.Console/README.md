# RadioProtocol.Console - RF320 Radio Control Application

Interactive console application for controlling RF320 BLE radio devices with keyboard control and command-line automation.

## Features

- **Interactive Mode**: Keyboard-driven control with real-time status display
- **Command-Line Mode**: Automated command execution for testing
- **Cross-Platform**: Runs on Windows (native BLE) and Linux (BlueZ)
- **Status Monitoring**: Real-time band, frequency, volume, and signal strength
- **Comprehensive Logging**: All protocol messages logged to file
- **Non-Scrolling UI**: Status header updates without scrolling in interactive mode

## Building

### Linux (Raspberry Pi)
```bash
cd csharp/src/RadioProtocol.Console
dotnet build --framework net8.0
```

### Windows
```bash
cd csharp/src/RadioProtocol.Console
dotnet build --framework net8.0-windows10.0.22621.0
```

Or simply:
```bash
dotnet build
```
(Will automatically select the appropriate framework for your platform)

## Running

### Interactive Mode (Keyboard Control)

```bash
dotnet run
```

The application will:
1. Scan for RF320 devices
2. Connect automatically to the first RF320 found
3. Send handshake
4. Display keyboard control interface
5. Show live status updates at the top of screen

### Command-Line Mode (Automated)

Pass action names as command-line arguments:

```bash
# Single command
dotnet run -- Band

# Multiple commands
dotnet run -- VolAdd VolAdd VolDel

# Frequency entry: 146.52 MHz
dotnet run -- Number1 Number4 Number6 Point Number5 Number2 FreqConfirm
```

Available actions: `Band`, `Number0-Number9`, `VolAdd`, `VolDel`, `UpShort`, `UpLong`, `DownShort`, `DownLong`, `Power`, `FreqConfirm`, `Point`, `Back`, and many more.

See `CanonicalAction` enum in `RadioProtocol.Core/Protocol/CanonicalAction.cs` for complete list.

## Keyboard Controls

### Interactive Mode Keys

#### Numbers
- **0-9** or **NumPad 0-9**: Send number commands
- **Ctrl+0-9**: Send number hold commands

#### Navigation
- **↑/↓**: Frequency up/down (short press)
- **Shift+↑/↓**: Frequency up/down (long press)

#### Volume
- **+**: Volume up
- **-**: Volume down

#### Radio Functions
| Key | Function | Key | Function |
|-----|----------|-----|----------|
| **B** | Band | **M** | Music |
| **P** | Power | **L** | Play |
| **S** | Step | **C** | Circle |
| **T** | Sub-Band | **Q** | SQ (Squelch) |
| **R** | Record | **D** | Demodulation |
| **W** | BandWidth | **O** | Display Mode |
| **E** | Stereo | **Y** | De-Emphasis |
| **X** | Preset | **N** | Memo |
| **U** | Bluetooth | | |

#### Special Keys
- **.**: Decimal point
- **Enter**: Frequency confirm
- **Backspace**: Back
- **Spacebar**: Music cycle
- **Escape**: Exit application

## Status Display

The top of the screen shows real-time radio status:

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Band: VHF    │ Freq: 145.095 MHz    │ Vol: 12  │ Signal: [████░░] │
└─────────────────────────────────────────────────────────────────────────┘
```

Updates automatically when:
- Band changes
- Frequency changes
- Volume changes
- Signal strength changes

## Logging

All protocol messages are logged to:
- **Linux**: `~/.local/share/RadioProtocol/Logs/RadioProtocol_YYYYMMDD_HHmmss.log`
- **Windows**: `%LOCALAPPDATA%\RadioProtocol\Logs\RadioProtocol_YYYYMMDD_HHmmss.log`

The log includes:
- All sent and received frames
- Status message details
- State updates (frequency, band, signal)
- Connection events
- Errors and warnings

## Troubleshooting

### No Device Found
- Ensure RF320 is powered on and advertising
- Check Bluetooth is enabled
- Try moving closer to the radio

### Connection Failed
- Restart the radio
- Restart Bluetooth service
- On Linux: `sudo systemctl restart bluetooth`
- On Windows: Check Bluetooth & devices settings

### Commands Not Working
- Check log file for errors
- Verify handshake completed
- Ensure WriteWithResponse is enabled (already configured correctly)

## Architecture

```
RadioProtocol.Console
├── Program.cs              - Main application, scanning, connection
├── KeyboardMapper.cs       - Keyboard → CanonicalAction mapping
└── appsettings.json       - Configuration (optional)

RadioProtocol.Core
├── Protocol/
│   ├── RadioFrame.cs      - Frame parsing and building
│   ├── CanonicalAction.cs - Action definitions and ID mapping
│   ├── StatusMessage.cs   - Status message parsing
│   └── RadioConnection.cs - High-level radio connection manager
└── Bluetooth/
    ├── IRadioTransport.cs          - Transport abstraction
    ├── WindowsBluetoothConnection.cs - Windows BLE implementation
    └── LinuxBluetoothConnection.cs   - Linux BlueZ implementation
```

## Protocol Details

### Frame Format
```
Standard Command: [AB 02 GG CC XX]
  AB = Header
  02 = Protocol version
  GG = Command group (0C=Button, 12=Ack, 1C=Status)
  CC = Command ID
  XX = Checksum (Base + CommandID)

Handshake: [AB 01 FF AB]

Status Messages: [AB LEN 1C TYPE DATA...]
```

### BLE Characteristics
- **TX (Write)**: `0000ff13-0000-1000-8000-00805f9b34fb`
- **RX (Notify)**: `0000ff14-0000-1000-8000-00805f9b34fb`
- **Service**: `0000ff12-0000-1000-8000-00805f9b34fb`

### Critical Implementation Details
- Must use `WriteWithResponse` for commands (not `WriteWithoutResponse`)
- Device does NOT send ACK to handshake - instead streams status
- Status messages (0x1C) arrive continuously (~2-3/sec)
- Frequency is encoded in nibbles of bytes 4-7 in ab0901 messages

## License

This project is licensed under the MIT License.
