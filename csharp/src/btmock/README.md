# btmock - Bluetooth Mock Radio Device

A Windows C# console application that acts as a mock Bluetooth LE radio device for protocol reverse engineering and testing.

## Overview

`btmock` simulates a Bluetooth Low Energy peripheral device (specifically mimicking an RF320 radio) to help analyze and understand the communication protocol between a controller application and the radio hardware. This tool is invaluable for:

- **Protocol Analysis**: Capture and log all messages from controller apps
- **Response Testing**: Send custom or canned responses to test controller behavior
- **Debugging**: Test controller applications without physical hardware
- **Documentation**: Create annotated logs of protocol interactions

## Features

### Core Functionality
- ✅ **BLE Peripheral Advertising**: Advertises as a configurable Bluetooth LE device
- ✅ **GATT Service**: Implements write and notify characteristics for bidirectional communication
- ✅ **Message Logging**: Logs all received messages with timestamps, raw data (hex + text), and user tags
- ✅ **CSV Export**: Writes logs to CSV format for easy analysis in Excel or other tools
- ✅ **Interactive Console**: Real-time message tagging and response sending
- ✅ **Canned Responses**: Pre-configured responses for common protocol messages
- ✅ **Custom Responses**: Enter arbitrary hex strings to send to connected controllers
- ✅ **Async Operations**: Non-blocking I/O throughout for responsive operation
- ✅ **Error Handling**: Robust error handling with clear console messages

### Configuration
All Bluetooth parameters are easily configurable via `config.json`:
- Device Name (default: "RF320-BLE")
- Device Address (default: "D5:D6:2A:FF:42:41")
- Service UUID (default: "0000ff10-0000-1000-8000-00805f9b34fb")
- Write Characteristic UUID (default: "0000ff13-0000-1000-8000-00805f9b34fb")
- Notify Characteristic UUID (default: "0000ff14-0000-1000-8000-00805f9b34fb")
- CSV log file path
- Canned responses

## Prerequisites

- **Operating System**: Windows 10 version 1803 or later (for Bluetooth LE peripheral support)
- **.NET**: .NET 8.0 SDK or later
- **Bluetooth**: Built-in or USB Bluetooth 4.0+ adapter
- **Visual Studio** (optional): For development and debugging

## Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd RaddyRF320BT/csharp
   ```

2. **Build the project**:
   ```bash
   dotnet build src/btmock
   ```

3. **Run the application**:
   ```bash
   dotnet run --project src/btmock
   ```

   Or navigate to the output directory and run directly:
   ```bash
   cd src/btmock/bin/Debug/net8.0-windows10.0.22621.0
   btmock.exe
   ```

## Usage

### Starting the Mock Radio

1. Run the application (see Installation above)
2. The mock radio will start advertising as "RF320-BLE" (or your configured name)
3. Use your controller application to scan for and connect to the device
4. Messages will be logged automatically as they arrive

### Interactive Commands

While the application is running, you can use these keyboard shortcuts:

| Key | Action | Description |
|-----|--------|-------------|
| `t` | Set Tag | Enter a description/tag for subsequent logged messages |
| `c` | Clear Tag | Clear the current message tag |
| `r` | Send Response | Send the "HandshakeResponse" canned response |
| `s` | Send Status | Send the "StatusResponse" canned response |
| `h` | Send Hex | Enter a custom hex string to send (e.g., "AB0C010203") |
| `i` | Instructions | Display the instructions again |
| `q` | Quit | Exit the application |

### Example Workflow

1. **Start the mock radio**:
   ```bash
   dotnet run --project src/btmock
   ```

2. **Connect your controller app** to the "RF320-BLE" device

3. **Tag incoming messages**:
   - Press `t`
   - Enter "Testing power button"
   - Press Enter
   - All subsequent messages will be tagged with this description

4. **Trigger actions in your controller** (e.g., press the power button)

5. **Send a response**:
   - Press `r` to send a handshake response
   - Or press `h` to enter a custom hex string like "AB0117AB"

6. **Review the logs**:
   - Open `logs/btmock_log.csv` in Excel or a text editor
   - See all captured messages with timestamps, hex data, text representation, and your tags

### Sample Console Output

```
===========================================
  Bluetooth Mock Radio Device (btmock)
===========================================

Configuration:
  Device Name:        RF320-BLE
  Device Address:     D5:D6:2A:FF:42:41
  Service UUID:       0000ff10-0000-1000-8000-00805f9b34fb
  Write Char UUID:    0000ff13-0000-1000-8000-00805f9b34fb
  Notify Char UUID:   0000ff14-0000-1000-8000-00805f9b34fb

Starting Bluetooth LE peripheral...

✓ Mock radio is now advertising and ready for connections!

============================================================
INSTRUCTIONS:
============================================================
  [t] - Set/change message tag for logging
  [c] - Clear current message tag
  [r] - Send canned response (handshake)
  [s] - Send canned response (status)
  [h] - Send custom hex string as response
  [i] - Display these instructions again
  [q] - Quit the application
============================================================

[2025-10-28 10:15:23] Connection status: Connected
  Device: Simulated-Device-ID

[2025-10-28 10:15:24.123] Received message:
  Tag: Testing power button
  Hex: AB 02 0C 14 CB
  Bytes: 5
  Text: .....
```

## Configuration File

Edit `config.json` to customize the mock radio:

```json
{
  "BluetoothConfig": {
    "DeviceName": "RF320-BLE",
    "DeviceAddress": "D5:D6:2A:FF:42:41",
    "ServiceUUID": "0000ff10-0000-1000-8000-00805f9b34fb",
    "WriteCharacteristicUUID": "0000ff13-0000-1000-8000-00805f9b34fb",
    "NotifyCharacteristicUUID": "0000ff14-0000-1000-8000-00805f9b34fb"
  },
  "Logging": {
    "CsvLogPath": "logs/btmock_log.csv",
    "EnableConsoleLogging": true
  },
  "CannedResponses": {
    "HandshakeResponse": "AB0117AB",
    "StatusResponse": "AB0417010203BC"
  }
}
```

### Configuration Options

- **DeviceName**: The Bluetooth device name that appears during scanning
- **DeviceAddress**: The MAC address (note: may be overridden by OS)
- **ServiceUUID**: The GATT service UUID containing the characteristics
- **WriteCharacteristicUUID**: Characteristic for receiving data from controllers
- **NotifyCharacteristicUUID**: Characteristic for sending data to controllers
- **CsvLogPath**: Path to the CSV log file (directory created automatically)
- **CannedResponses**: Pre-defined hex strings for quick testing

## CSV Log Format

The log file contains the following columns:

| Column | Description | Example |
|--------|-------------|---------|
| Timestamp | ISO 8601 timestamp with milliseconds | 2025-10-28 10:15:24.123 |
| DataHex | Hex representation of received bytes | AB 02 0C 14 CB |
| DataText | Printable text (non-printable = '.') | ..... |
| ByteCount | Number of bytes in the message | 5 |
| UserTag | User-provided description | Testing power button |

Example CSV content:
```csv
Timestamp,DataHex,DataText,ByteCount,UserTag
2025-10-28 10:15:24.123,AB 02 0C 14 CB,.....,5,Testing power button
2025-10-28 10:15:25.456,AB 02 0C 12 C9,.....,5,Volume up test
```

## Architecture

### Project Structure
```
btmock/
├── Bluetooth/                      # Bluetooth LE peripheral implementation
│   ├── BluetoothPeripheral.cs     # Main peripheral/GATT server logic
│   ├── ConnectionStatusEventArgs.cs
│   └── MessageReceivedEventArgs.cs
├── Config/                         # Configuration classes
│   └── BluetoothConfiguration.cs  # Configuration data model
├── Logging/                        # Message logging
│   └── MessageLogger.cs           # CSV logging implementation
├── Program.cs                      # Main application entry point
├── config.json                     # Configuration file
├── btmock.csproj                   # Project file
└── README.md                       # This file
```

### Key Components

1. **Program.cs**: Main application loop, console interaction, and event handling
2. **BluetoothPeripheral.cs**: Bluetooth LE advertising and GATT service implementation
3. **MessageLogger.cs**: CSV file logging with thread-safe operations
4. **BluetoothConfiguration.cs**: Strongly-typed configuration model

### Technology Stack

- **.NET 8.0**: Modern C# with async/await patterns
- **Windows.Devices.Bluetooth**: Native Windows BLE peripheral support (UWP API)
- **InTheHand.BluetoothLE**: Cross-platform Bluetooth library (fallback)
- **CsvHelper**: CSV file writing and reading
- **Microsoft.Extensions.Configuration**: JSON configuration management
- **Microsoft.Extensions.Logging**: Structured logging

## Troubleshooting

### Common Issues

#### "Cannot build: Not on Windows"
- **Problem**: Building on a non-Windows system
- **Solution**: This project requires Windows for Bluetooth LE peripheral features
- **Workaround**: Use a Windows VM or dual-boot system

#### "Bluetooth adapter not found"
- **Problem**: No Bluetooth adapter or adapter disabled
- **Solution**: 
  - Check Device Manager for Bluetooth adapters
  - Enable Bluetooth in Windows Settings
  - Install/update Bluetooth drivers

#### "Access denied" errors
- **Problem**: Application may need elevated privileges
- **Solution**: 
  - Run as Administrator (first time only)
  - Check Windows Bluetooth permissions
  - Ensure Bluetooth privacy settings allow app access

#### CSV file not created
- **Problem**: Insufficient permissions or invalid path
- **Solution**: 
  - Check that the application can write to the logs directory
  - Verify the path in config.json is valid
  - Try using an absolute path (e.g., "C:\\Logs\\btmock.csv")

#### No connections from controller
- **Problem**: Controller cannot discover the mock device
- **Solution**:
  - Verify Bluetooth is enabled on both devices
  - Check that device name matches what controller is scanning for
  - Ensure UUIDs in config match what controller expects
  - Try scanning from Windows Bluetooth settings to verify advertising

### Debug Mode

To enable detailed diagnostic logging:

1. Edit the LogLevel in Program.cs:
   ```csharp
   builder.SetMinimumLevel(LogLevel.Debug);
   ```

2. Rebuild and run:
   ```bash
   dotnet build src/btmock
   dotnet run --project src/btmock
   ```

## Limitations & Notes

### Current Implementation
- **Simulated Connections**: The initial version includes simulated Bluetooth operations for testing the application structure
- **Platform-Specific**: Requires Windows 10+ for full BLE peripheral support
- **Single Connection**: Designed for one controller connection at a time
- **Placeholder UUIDs**: Default UUIDs should be replaced with actual radio protocol values

### Future Enhancements
Potential improvements for full production use:

- [ ] Full Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider implementation
- [ ] Actual GattLocalCharacteristic write event handling
- [ ] Multiple simultaneous connection support
- [ ] Protocol parsing and validation
- [ ] Auto-response based on received commands
- [ ] Graphical user interface (GUI)
- [ ] Real-time hex editor for response crafting
- [ ] Protocol state machine tracking
- [ ] Wireshark-style packet dissector

## Contributing

This project is part of the RaddyRF320BT repository. Contributions welcome!

### Making Changes
1. Fork the repository
2. Create a feature branch
3. Make your changes with appropriate comments
4. Test thoroughly on Windows 10/11
5. Submit a pull request

### Code Style
- Follow Microsoft C# coding conventions
- Use XML documentation comments for public APIs
- Keep classes focused and single-purpose
- Use async/await for all I/O operations
- Handle errors gracefully with informative messages

## License

This project is licensed under the MIT License - see the LICENSE file in the repository root for details.

## Related Documentation

- [Main Repository README](../../README.md)
- [Protocol Reference](../../docs/PROTOCOL_REFERENCE.md)
- [C# Implementation Guide](../README.md)
- [Wireshark Analysis](../../docs/WIRESHARK_ANALYSIS.md)

## Support

For questions, issues, or suggestions:
- **GitHub Issues**: Report bugs or request features
- **Documentation**: Refer to inline code comments
- **Examples**: See sample logs and configuration in this README

---

**Note**: This is a development tool for protocol reverse engineering. UUIDs, device names, and protocol details are placeholders that should be configured to match your specific device and use case.

*Built for the RaddyRF320BT protocol analysis project*
