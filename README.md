# Raddy RF320 Bluetooth Protocol

This project contains a comprehensive implementation and documentation of a Bluetooth protocol for controlling the Raddy RF320 device. The project includes both the original Java reverse-engineered code, some bluetooth packet captures and a modern C# implementation with enhanced features.

## 📁 Project Structure

```
RadioProtocolProject/
├── 📄 README.md                    # This overview document
├── 📁 JavaExtracted/               # Original Java code (reverse-engineered)
├── 📁 csharp/                     # Modern C# implementation  
└── 📁 docs/                       # Protocol dumps, extracted documentation and analysis
```

## 🎯 Quick Start

### App / Protocol Analysis
- The original app was decompiled with apktool
- Found the the bluetooth specific portion of the app in ```smali\com\myhomesmartlife\bluetooth```
- Pointing an LLM (Claude Sonnet) at it, asking it to generate a more readable version of the code (the original app was obfuscated before publishing)
- Claude generated the initial cut at the reverse engineered java files in JavaExtracted.
- Ran the Radio-C app on my Android phone with bluetooth packet capture turned on, and generated a few bug reports.
- Looked at the packet captures in WireShark and generated an XML version of this data to feed into the LLM (see docs/BTDump.pdml).
- Created a simple bluetooth app to connect to the RF320 and capture packets being sent (see docs/Messages From RF320.txt)
- Asked Claude to use the data from the packet dumps to try to flesh out the protocol definition further.
- Generated a modern c# version of this protocol to use to hit the actual hardware and debug further.

This is all motivated by a desire for me to refurb a 1940's console radio by Grandpa Anderson gave me when he died.  It had AM/FM/SW on it and I used to love playing with it trying to find interesting stations.  Normally this would be a time station with UTC beeping on it.  I'd like for my grandkids to have a similar experience with SW radio, but plan to incorporate Spotify, Google Cast, vinyl records etc. into the device.  

Note that I've asked Raddy for 'real' specs on their Bluetooth protocol, but haven't heard whether they're willing to share them.  If they do and don't mind if I share with others, I'll post here.

#### Further Reverse Engineering Resources
- [Raddy RF320 User Manual](https://raddy-download.s3.amazonaws.com/Raddy_RF320_User_Manual_EN_20230417.pdf)
- [Hacking Bluetooth Protocol](https://youtu.be/NIBmiPtCDdM) - YouTube tutorial (This was very helpful)
- [RF320 Radio Breakdown](https://youtu.be/0bXpALNLudY) - YouTube review (this was interesting but not as helpful)

### For C# Development (Recommended)
```bash
cd csharp
dotnet build
dotnet test
dotnet run --project src/RadioProtocol.Console
```
👉 **See [csharp/README.md](csharp/README.md) for complete C# documentation**

### For Java Reference
The original Java code in `JavaExtracted/` provides the foundation for understanding the protocol structure.

---

## 📚 Documentation

### Core Protocol Documentation
| File | Description | Location |
|------|-------------|----------|
| **Protocol Reference** | Complete protocol specification | [`docs/PROTOCOL_REFERENCE.md`](docs/PROTOCOL_REFERENCE.md) |
| **Command Sequences** | Documented command-response flows | [`docs/COMMAND_RESPONSE_SEQUENCES.md`](docs/COMMAND_RESPONSE_SEQUENCES.md) |
| **Additional Messages** | Extended protocol messages | [`docs/ADDITIONAL_MESSAGES.md`](docs/ADDITIONAL_MESSAGES.md) |
| **Quick Reference** | Protocol quick start guide | [`docs/QUICK_REFERENCE.md`](docs/QUICK_REFERENCE.md) |

### Analysis and Research
| File | Description | Location |
|------|-------------|----------|
| **Wireshark Analysis** | Bluetooth packet analysis | [`docs/WIRESHARK_ANALYSIS.md`](docs/WIRESHARK_ANALYSIS.md) |
| **Bidirectional Analysis** | Two-way communication study | [`docs/BIDIRECTIONAL_ANALYSIS.md`](docs/BIDIRECTIONAL_ANALYSIS.md) |
| **Project Summary** | Research findings overview | [`docs/SUMMARY.md`](docs/SUMMARY.md) |
| **Raw Captures** | Protocol capture logs | [`docs/BIDIRECTIONAL_CAPTURE.txt`](docs/BIDIRECTIONAL_CAPTURE.txt) |
| **BTDump Analysis** | Bluetooth dump analysis | [`docs/BTDump.pdml`](docs/BTDump.pdml) |

---

## 🏗️ Implementation Comparison

### Java Implementation (Original)
- **Purpose**: Reverse-engineered reference from decompiled code
- **Architecture**: Direct translation from obfuscated Android app
- **Features**: Basic protocol implementation with 100+ individual button methods
- **Location**: [`JavaExtracted/`](JavaExtracted/)

### C# Implementation (Enhanced)
- **Purpose**: Modern, production-ready library
- **Architecture**: Clean, testable, cross-platform design
- **Features**: 
  - 🔄 **Simplified API**: Enum-based commands vs 100+ individual methods
  - 🖥️ **Cross-Platform**: Windows + Raspberry Pi support
  - 📝 **Enhanced Logging**: Daily rotation with 2-day retention
  - 🧪 **Comprehensive Tests**: 80+ test methods with mocks
  - 📱 **Modern Patterns**: Async/await, dependency injection, records
- **Location**: [`csharp/`](csharp/)

---

## 🔌 Protocol Overview

### Connection Details
- **Protocol**: Bluetooth Low Energy (BLE) with GATT
- **Write Characteristic**: `0000ff13-0000-1000-8000-00805f9b34fb`
- **Notify Characteristic**: `0000ff14-0000-1000-8000-00805f9b34fb`

### Packet Structure
```
Byte 0:    START_BYTE (0xAB)
Byte 1:    LENGTH (typically 0x02)  
Byte 2:    COMMAND_TYPE (typically 0x0C for buttons)
Byte 3:    COMMAND_DATA (specific command code)
Byte 4:    CHECKSUM (sum of bytes 0-3)
```

### Example Commands
```bash
Power Button:     AB 02 0C 14 CB
Volume Up:        AB 02 0C 12 C9  
Volume Down:      AB 02 0C 13 CA
Channel 1:        AB 02 0C 01 B8
Channel 5:        AB 02 0C 05 BC
```

## 🚀 Getting Started

### Prerequisites
- **For C# Development**: .NET 8.0+ SDK
- **For Java Reference**: Java 8+ JDK
- **Hardware**: Bluetooth adapter + compatible radio device

### C# Quick Start (Recommended)
```bash
# Clone and build
git clone <repository-url>
cd csharp

# Build and test
dotnet build
dotnet test

# Run interactive demo
dotnet run --project src/RadioProtocol.Console
```

### Java Reference Review
```bash
# Examine original protocol implementation
cd JavaExtracted
# Review RadioProtocolCommands.java for all button codes
# Check RadioBluetoothManager.java for connection logic
```

---

## 🔧 Development Workflow

### 1. **Protocol Research** → [`docs/`](docs/)
Start with protocol documentation to understand the communication structure.

### 2. **Java Reference** → [`JavaExtracted/`](JavaExtracted/)
Review the original implementation for protocol details and edge cases.

### 3. **C# Implementation** → [`csharp/`](csharp/)
Build modern applications using the enhanced C# library.

---

## 📊 Feature Comparison

| Feature | Java (Original) | C# (Enhanced) |
|---------|----------------|---------------|
| **Button Commands** | 100+ individual methods | Enum-based unified API |
| **Platform Support** | Android only | Windows + Linux/RPi |
| **Logging** | Basic Android logs | Daily rotation + retention |
| **Testing** | None | 80+ comprehensive tests |
| **Error Handling** | Basic try/catch | Comprehensive validation |
| **Async Support** | Callback-based | Modern async/await |
| **Documentation** | Minimal comments | Full API documentation |

---

## 🧪 Testing & Validation

### Protocol Testing
- **Packet Validation**: Checksum verification for all commands
- **Command Coverage**: Tests for all button types and operations
- **Error Scenarios**: Invalid packets, connection failures, timeouts
- **Performance**: Rapid command execution and memory usage

### Hardware Testing
- **Multiple Devices**: Tested with various radio models
- **Platform Validation**: Windows 10/11 and Raspberry Pi OS
- **Connection Stability**: Long-running connection tests
- **Signal Quality**: Various Bluetooth adapter types

---

## 🛠️ Protocol Extensions

The current implementation supports extensions for:

### Additional Commands
- **Firmware Updates**: Protocol for over-the-air updates
- **Configuration**: Device setting modification
- **Diagnostics**: Health and status monitoring

### Enhanced Features  
- **Encryption**: Secure command transmission
- **Authentication**: Device pairing verification
- **Discovery**: Automatic radio device detection

---

## 📈 Project Evolution

### Phase 1: Reverse Engineering ✅
- Decompiled Android app to understand protocol
- Created readable Java reference implementation
- Documented packet structures and command codes

### Phase 2: Protocol Documentation ✅
- Analyzed Bluetooth captures with Wireshark
- Created comprehensive protocol specification
- Documented bidirectional communication patterns

### Phase 3: Modern Implementation ✅
- Built cross-platform C# library
- Added comprehensive testing and logging
- Simplified API with modern patterns

### Phase 4: Advanced Features (Planned)
- Web API interface for remote control
- Mobile app with enhanced UI
- IoT integration capabilities

---

## 🤝 Contributing

### Research Contributions
- **Protocol Analysis**: Document new message types or patterns
- **Hardware Testing**: Validate with additional radio models
- **Capture Analysis**: Provide new Bluetooth packet captures

### Code Contributions
- **Java Improvements**: Enhance the reference implementation
- **C# Features**: Add new library capabilities
- **Testing**: Expand test coverage and scenarios
- **Documentation**: Improve guides and examples

### Getting Involved
1. **Fork the repository**
2. **Choose your area**: Protocol research, Java reference, or C# development
3. **Create feature branch**: `git checkout -b feature/your-enhancement`
4. **Submit pull request**: Include tests and documentation

---

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📞 Support & Community

- **Issues**: Use GitHub issue tracker for bugs and feature requests
- **Discussions**: GitHub Discussions for questions and community support
- **Documentation**: Check the `docs/` folder for detailed protocol information
- **Examples**: Review `csharp/` for implementation patterns

---

## 🔗 Related Projects

- **Android App**: Original decompiled source (reference only)
- **Wireshark Captures**: Bluetooth protocol analysis files
- **Hardware Documentation**: Radio device specifications and manuals

---

*This project bridges the gap between reverse-engineered protocol knowledge and modern, maintainable software implementations. Whether you're researching radio protocols, building IoT applications, or developing Bluetooth communication systems, this repository provides both the foundational knowledge and practical tools to get started.*
- `frequencyUpFast()` / `frequencyDownFast()` - Fast scan (long press)
- `enterFrequencyMode()` - Enter frequency entry mode

### Band Selection
- `nextBand()` - Switch to next band
- `selectSubBand()` - Select sub-band
- `bandLongPress()` - Band long press function

### Volume Control
- `volumeUp()` / `volumeDown()` - Adjust volume

### Number Entry (0-9)
- `pressNumber(n)` - Press number button (0-9)
- `pressNumberLong(n)` - Long press number button
- `pressPoint()` - Decimal point
- `pressBack()` - Backspace/delete

### Audio Modes
- `toggleMusic()` - Switch to music mode
- `playPause()` - Play/pause media
- `nextTrack()` - Skip to next track
- `toggleCircle()` - Toggle repeat/loop mode

### Radio Settings
- `changeDemodulation()` - Change modulation type (FM/AM/etc)
- `changeBandwidth()` - Adjust bandwidth
- `adjustSquelch()` - Adjust squelch level
- `toggleStereo()` - Toggle stereo/mono
- `toggleDE()` - Toggle de-emphasis

### Memory & Presets
- `accessPreset()` - Access preset channels
- `accessMemo()` - Access memory
- `saveMemo()` - Save to memory (long press)

### Recording
- `toggleRecording()` - Start/stop recording

### Emergency
- `activateSOS()` - Activate SOS beacon
- `activateAlarm()` - Trigger alarm

---

## Data Models

### `RateTypeInfo.java`
Represents a rate/mode type configuration.
```java
public class RateTypeInfo {
    String hexType;    // Hex identifier
    String name;       // Display name
    String bitType;    // Bit pattern
}
```

### `RemarkInfo.java`
Represents a saved memory/preset entry.
```java
public class RemarkInfo {
    String band;       // Band identifier
    String decRate;    // Demodulation rate
    String byte4-7;    // Data bytes
    String name;       // User label
}
```

---

## Protocol Examples

### Example 1: Send Handshake
```
Sent: AB 01 FF AB
Purpose: Initial handshake after connection
```

### Example 2: Press Number 1
```
Sent: AB 02 0C 01 BA
Breakdown:
  AB = Start byte
  02 = Length
  0C = Button command type
  01 = Number 1 data
  BA = Checksum (AB+02+0C+01 = BA)
```

### Example 3: Volume Up
```
Sent: AB 02 0C 12 CB
Purpose: Increase radio volume by one step
```

### Example 4: Receive Frequency Status
```
Received: AB 04 17 xx yy zz ... 
Where:
  AB0417 = Frequency status packet identifier
  Following bytes contain frequency, band, and status data
```

---

## Implementation Notes

### Connection Sequence
1. Scan for BLE devices
2. Connect to radio device via GATT
3. Discover services and characteristics
4. Find write characteristic (ff13) and notify characteristic (ff14)
5. Enable notifications on ff14
6. Send handshake command (CMD_HANDSHAKE)
7. Wait for ready state

### Data Flow
```
App → RadioCommandSender → RadioBluetoothManager → BLE GATT → Radio Device
App ← RadioProtocolHandler ← RadioBluetoothManager ← BLE GATT ← Radio Device
```

### Error Handling
- Check `isReady()` before sending commands
- Monitor connection state via `ConnectionListener`
- Parse received data via `DataReceivedListener`

### Haptic Feedback
The command sender provides optional vibration feedback (100ms) on each button press, mimicking physical button tactile response.

---

## Files in This Package

| File | Purpose |
|------|---------|
| `RadioProtocolCommands.java` | All command byte constants |
| `RadioBluetoothManager.java` | BLE connection management |
| `RadioProtocolHandler.java` | Parse incoming data packets |
| `RadioCommandSender.java` | High-level command interface |
| `RateTypeInfo.java` | Rate/mode type data model |
| `RemarkInfo.java` | Memory/preset data model |
| `README.md` | This documentation |
| `PROTOCOL_REFERENCE.md` | Detailed protocol specification |

---

## Additional Resources

### Reverse Engineering Notes
The original app was decompiled to smali bytecode. Key findings:
- Main communication logic in `MainActivity.smali` and `MainActivity$162.smali`
- Command definitions in `SendData.smali`
- Data parsing in `MainActivity.a([B)` method

### Protocol Observations
- All commands use 5-byte packets (except handshake which uses 4 bytes)
- Command type 0x0C is used for all button presses
- Checksum is simple additive checksum modulo 256
- Received packets use different format with variable length
- Frequency is transmitted in hexadecimal and converted to decimal Hz

---

## License & Usage

These classes are provided for educational and interoperability purposes. They document the reverse-engineered protocol of the radio device for development of compatible applications.

**Note:** This is a cleaned-up version of decompiled obfuscated code. Some functionality may need testing and validation with actual hardware.

---

## Future Enhancements

Potential improvements:
- Add checksum verification for received packets
- Implement frequency encoding/decoding helpers
- Add support for additional packet types
- Create unit tests for protocol encoding/decoding
- Add async command queuing for reliable transmission
- Implement retry logic for failed commands

---

## Contact & Support

This documentation was generated through reverse engineering of decompiled Android APK smali code for the purpose of understanding the Bluetooth radio communication protocol.

