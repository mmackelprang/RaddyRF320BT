# Radio Protocol Library - C# Implementation

A modern, cross-platform C# library for controlling radio devices via Bluetooth using a custom protocol. This implementation provides a clean, testable, and maintainable solution with comprehensive logging and error handling.

## 🏗️ Architecture Overview

The C# implementation follows modern .NET patterns with clean separation of concerns:

```
RadioProtocol.Core              📦 Core Library
├── 📁 Bluetooth/               ┌─ Cross-platform Bluetooth abstraction
├── 📁 Commands/                ├─ Protocol command builders  
├── 📁 Constants/               ├─ Protocol constants and enums
├── 📁 Logging/                 ├─ Enhanced logging with context
├── 📁 Models/                  ├─ Data models and records
├── 📁 Protocol/                └─ Protocol parsing and utilities
└── RadioManager.cs             ⚡ Main API entry point

RadioProtocol.Console           🖥️ Demo Application  
└── Program.cs                  ┌─ Interactive console demo
                                └─ Configuration and DI setup

btmock                          🔧 Mock Radio Device (NEW!)
├── 📁 Bluetooth/               ┌─ BLE peripheral implementation
├── 📁 Config/                  ├─ Configuration classes
├── 📁 Logging/                 ├─ CSV message logging
└── Program.cs                  └─ Interactive mock radio console

RadioProtocol.Tests             🧪 Test Suite
├── 📁 Mocks/                   ┌─ Mock implementations
├── 📁 Integration/             ├─ Integration tests
├── 📁 EndToEnd/                ├─ Complete workflow tests
└── 📁 Utilities/               └─ Test helpers and data
```

## 📚 Library Components

### Core Library (`RadioProtocol.Core`)

#### 🎯 **RadioManager** - Main API
- **Purpose**: Primary interface for radio communication
- **Features**: Connection management, command sending, event handling
- **Usage**: `var manager = new RadioManagerBuilder().WithFileLogging("logs/radio.log").Build();`

#### 🔗 **Bluetooth Abstraction** 
- **Windows**: Uses `Windows.Devices.Bluetooth` for native Windows support
- **Linux/Raspberry Pi**: Uses `Iot.Device.Bindings` for GPIO and Bluetooth
- **Interface**: `IBluetoothConnection` for testability and platform independence

#### 📝 **Enhanced Logging**
- **Daily Log Files**: Automatic daily rotation (`radio_2025-10-24.log`)
- **2-Day Retention**: Automatic cleanup of files older than 2 days
- **Context Capture**: Automatic class/method names using `[CallerMemberName]`
- **Structured**: Raw data, messages, errors with timestamps

#### 🎛️ **Protocol Commands**
- **Simplified Design**: Enum-based button types vs 100+ individual Java methods
- **Type Safety**: Strong typing for all protocol constants
- **Checksum Validation**: Automatic checksum calculation and verification
- **Error Handling**: Comprehensive validation and error reporting

#### 📊 **Data Models**
Modern C# record types for immutable data:
- `RadioStatus` - Current radio state and settings
- `DeviceInfo` - Radio device information
- `ConnectionInfo` - Bluetooth connection status
- `CommandResult` - Command execution results

### Console Application (`RadioProtocol.Console`)

#### 🖥️ **Interactive Demo**
```
Radio Protocol Library - Console Demo
=====================================

Available Commands:
1. Connect to radio
2. Send button press
3. Send channel command
4. Request status
5. Run automated demo
6. View connection status
7. Exit

Choose option (1-7):
```

#### ⚙️ **Configuration**
JSON-based configuration with dependency injection:
```json
{
  "RadioSettings": {
    "LogFilePath": "logs/radio-protocol.log",
    "DefaultDevice": "00:11:22:33:44:55",
    "ConnectionTimeout": 30000
  }
}
```

### Mock Radio Device (`btmock`) - NEW!

#### 🔧 **Bluetooth Mock Radio**
A Windows console application that acts as a mock Bluetooth LE radio device for protocol reverse engineering and testing.

**Key Features:**
- **BLE Peripheral**: Advertises as a configurable Bluetooth LE device (default: "RF320-BLE")
- **GATT Service**: Implements write and notify characteristics for bidirectional communication
- **Message Logging**: Logs all received messages to CSV with timestamps, hex data, and user tags
- **Interactive Console**: Real-time message tagging and response sending
- **Canned Responses**: Pre-configured responses for common protocol messages
- **Custom Responses**: Enter arbitrary hex strings to send to connected controllers

**Usage:**
```bash
# Run the mock radio
dotnet run --project src/btmock

# Press keys for actions:
# [t] - Set message tag
# [c] - Clear tag
# [r] - Send handshake response
# [s] - Send status response
# [h] - Send custom hex
# [q] - Quit
```

**Configuration:**
All parameters are easily configurable via `config.json`:
- Device Name, Address, and UUIDs
- CSV log file path
- Canned response definitions

👉 **See [src/btmock/README.md](src/btmock/README.md) for complete btmock documentation**

### Test Suite (`RadioProtocol.Tests`)

#### 🧪 **Comprehensive Testing**
- **80+ Test Methods** covering all scenarios
- **Mock Implementations** for hardware-independent testing
- **Real Protocol Data** using documented message sequences
- **Performance Testing** for rapid command execution
- **Concurrency Testing** for thread safety

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0 or later**
- **Windows 10/11** (for Windows Bluetooth) or **Linux** (for Raspberry Pi)
- **Bluetooth adapter** for radio communication

### Installation

1. **Clone the repository:**
```bash
git clone <repository-url>
cd CSharpImplementation
```

2. **Build the solution:**
```bash
dotnet build
```

3. **Run tests:**
```bash
dotnet test
```

4. **Run the console demo:**
```bash
dotnet run --project src/RadioProtocol.Console
```

### Quick Start Example

```csharp
using RadioProtocol.Core;
using RadioProtocol.Core.Constants;

// Create radio manager with file logging
var radioManager = new RadioManagerBuilder()
    .WithFileLogging("logs/radio.log")
    .Build();

// Connect to radio
await radioManager.ConnectAsync("00:11:22:33:44:55");

// Send button press
await radioManager.SendButtonPressAsync(ButtonType.Power);

// Send channel command
await radioManager.SendChannelCommandAsync(5);

// Request status
await radioManager.SendStatusRequestAsync();

// Disconnect
await radioManager.DisconnectAsync();
```

## 🔧 Key Features

### 1. **Cross-Platform Support**
- **Windows**: Native Bluetooth LE support
- **Raspberry Pi**: GPIO and Linux Bluetooth support
- **Abstracted Interface**: Same API across platforms

### 2. **Modern C# Patterns**
- **Async/Await**: Non-blocking operations throughout
- **Dependency Injection**: Clean separation of concerns  
- **Record Types**: Immutable data structures
- **Pattern Matching**: Modern C# language features

### 3. **Enhanced Logging**
```
[2025-10-24 10:30:15.123] [INFO] [RadioManager.ConnectAsync] Connecting to device: 00:11:22:33:44:55
[2025-10-24 10:30:15.456] [INFO] [RadioManager.ConnectAsync] RAW SENT: AB0C01140A (5 bytes)
[2025-10-24 10:30:15.789] [INFO] [RadioManager.ProcessResponse] RAW RECEIVED: AB12010101 (5 bytes)
```

### 4. **Comprehensive Error Handling**
- **Connection Errors**: Automatic retry and recovery
- **Protocol Errors**: Invalid message detection
- **Hardware Errors**: Bluetooth adapter issues
- **Graceful Degradation**: Continues operation when possible

### 5. **Simplified Protocol**
**Java (Original):**
```java
// 100+ individual button methods
radioProtocol.sendPowerButton();
radioProtocol.sendVolumeUpButton();
radioProtocol.sendChannel1Button();
// ... many more
```

**C# (Improved):**
```csharp
// Enum-based approach
await radioManager.SendButtonPressAsync(ButtonType.Power);
await radioManager.SendButtonPressAsync(ButtonType.VolumeUp);
await radioManager.SendChannelCommandAsync(1);
```

## 📁 Project Structure

```
CSharpImplementation/
├── 📄 RadioProtocol.sln                 # Solution file
├── 📁 src/
│   ├── 📁 RadioProtocol.Core/           # Core library
│   │   ├── 📁 Bluetooth/
│   │   │   ├── IBluetoothConnection.cs
│   │   │   ├── WindowsBluetoothConnection.cs
│   │   │   └── LinuxBluetoothConnection.cs
│   │   ├── 📁 Commands/
│   │   │   └── RadioCommandBuilder.cs
│   │   ├── 📁 Constants/
│   │   │   ├── ProtocolConstants.cs
│   │   │   ├── ButtonType.cs
│   │   │   ├── ConnectionState.cs
│   │   │   └── ResponsePacketType.cs
│   │   ├── 📁 Logging/
│   │   │   ├── IRadioLogger.cs
│   │   │   ├── RadioLogger.cs
│   │   │   ├── FileLogger.cs
│   │   │   └── FileLoggerProvider.cs
│   │   ├── 📁 Models/
│   │   │   ├── RadioStatus.cs
│   │   │   ├── DeviceInfo.cs
│   │   │   ├── ConnectionInfo.cs
│   │   │   ├── CommandResult.cs
│   │   │   └── [12 other model files]
│   │   ├── 📁 Protocol/
│   │   │   └── RadioProtocolParser.cs
│   │   ├── RadioManager.cs
│   │   └── RadioProtocol.Core.csproj
│   └── 📁 RadioProtocol.Console/
│       ├── Program.cs
│       ├── appsettings.json
│       └── RadioProtocol.Console.csproj
├── 📁 tests/
│   └── 📁 RadioProtocol.Tests/
│       ├── 📁 Mocks/
│       │   ├── MockBluetoothConnection.cs
│       │   └── MockRadioLogger.cs
│       ├── 📁 Integration/
│       │   └── RadioManagerIntegrationTests.cs
│       ├── 📁 EndToEnd/
│       │   └── EndToEndTests.cs
│       ├── 📁 Commands/
│       │   └── RadioCommandBuilderTests.cs
│       ├── 📁 Protocol/
│       │   └── RadioProtocolParserTests.cs
│       └── RadioProtocol.Tests.csproj
└── 📄 README.md                        # This file
```

## 🔬 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "ClassName=RadioManagerIntegrationTests"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
1. **Unit Tests** - Individual component testing
2. **Integration Tests** - Component interaction testing
3. **End-to-End Tests** - Complete workflow testing
4. **Performance Tests** - Load and timing validation
5. **Mock Tests** - Hardware-independent validation

## 📊 Performance

### Benchmarks
- **Individual Commands**: <10ms average execution time
- **100 Rapid Commands**: <5 seconds total execution time
- **Memory Usage**: <50MB for typical operations
- **Log File Size**: ~1MB per day with moderate usage

### Optimization Features
- **Connection Pooling**: Reuse Bluetooth connections
- **Command Queuing**: Efficient batch processing
- **Lazy Loading**: On-demand resource allocation
- **Memory Management**: Proper disposal patterns

## 🔧 Configuration

### Logging Configuration
```csharp
var radioManager = new RadioManagerBuilder()
    .WithFileLogging("logs/radio-protocol.log")  // Daily rotation + 2-day retention
    .Build();
```

### Custom Logger
```csharp
var customLogger = new MyCustomLogger();
var radioManager = new RadioManagerBuilder()
    .WithLogger(customLogger)
    .Build();
```

### Platform-Specific Setup

#### Windows
- Requires Windows 10 Version 1803 or later
- Bluetooth LE support required
- May require administrator privileges for first connection

#### Raspberry Pi (Linux)
```bash
# Install required packages
sudo apt-get update
sudo apt-get install bluetooth libbluetooth-dev

# Enable Bluetooth service
sudo systemctl enable bluetooth
sudo systemctl start bluetooth
```

## 🐛 Troubleshooting

### Common Issues

#### Connection Problems
- **Issue**: Cannot connect to radio device
- **Solution**: Check device pairing and Bluetooth adapter status
- **Logs**: Check daily log files for connection error details

#### Permission Errors
- **Issue**: Access denied when writing log files
- **Solution**: Ensure application has write permissions to log directory
- **Alternative**: Use a different log path (e.g., user's temp directory)

#### Performance Issues
- **Issue**: Slow command execution
- **Solution**: Check Bluetooth signal strength and reduce interference
- **Monitoring**: Use performance tests to identify bottlenecks

### Debug Mode
```csharp
// Enable debug logging
var radioManager = new RadioManagerBuilder()
    .WithFileLogging("logs/debug.log")
    .Build();

// All operations will be logged with full context
```

## 🛣️ Roadmap

### Planned Features
- [ ] **WebAPI Interface** - REST API for web applications
- [ ] **gRPC Support** - High-performance RPC communication
- [ ] **Configuration UI** - Web-based configuration interface
- [ ] **Device Discovery** - Automatic radio device detection
- [ ] **Firmware Updates** - Over-the-air radio firmware updates

### Version History
- **v1.0** - Initial C# implementation with basic protocol support
- **v1.1** - Added cross-platform Bluetooth support
- **v1.2** - Enhanced logging with daily rotation
- **v1.3** - Comprehensive test suite and mock implementations
- **v1.4** - Single class per file architecture (current)

## 🤝 Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8.0 SDK
3. Open in Visual Studio or VS Code
4. Run `dotnet build` to verify setup

### Code Standards
- Follow Microsoft C# coding conventions
- Use meaningful names and comprehensive documentation
- Write tests for new features
- Maintain single class per file structure

### Pull Request Process
1. Create feature branch from `main`
2. Implement changes with tests
3. Update documentation if needed
4. Submit pull request with clear description

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📞 Support

For questions, issues, or contributions:
- **Issues**: Use GitHub issue tracker
- **Documentation**: Refer to inline code documentation
- **Examples**: See console application for usage patterns

---

*Built with ❤️ using modern C# and .NET 8.0*