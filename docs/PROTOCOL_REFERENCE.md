# Radio Bluetooth Protocol - Technical Reference

## Table of Contents
1. [Protocol Overview](#protocol-overview)
2. [GATT Profile](#gatt-profile)
3. [Command Format](#command-format)
4. [Command Reference](#command-reference)
5. [Response Format](#response-format)
6. [Data Types](#data-types)
7. [State Machine](#state-machine)

---

## Protocol Overview

### Technology
- **Transport:** Bluetooth Low Energy (BLE) 4.0+
- **Profile:** GATT (Generic Attribute Profile)
- **Role:** Client (phone/app) to Server (radio device)
- **Data Rate:** Low throughput, command/response pattern

### Characteristics
- **Packet Size:** 4-5 bytes for commands, variable for responses
- **Endianness:** Little-endian for multi-byte values
- **Encoding:** Binary with hex representation
- **Error Detection:** Simple additive checksum

---

## GATT Profile

### Service Structure
```
Service (Primary)
├── Characteristic: Write (ff13)
│   ├── Properties: Write Without Response
│   └── Purpose: Send commands to radio
│
└── Characteristic: Notify (ff14)
    ├── Properties: Notify
    ├── Descriptor: Client Characteristic Configuration (2902)
    └── Purpose: Receive data from radio
```

### UUIDs
```
Write Characteristic:  0000ff13-0000-1000-8000-00805f9b34fb
Notify Characteristic: 0000ff14-0000-1000-8000-00805f9b34fb
CCCD Descriptor:       00002902-0000-1000-8000-00805f9b34fb
```

### Connection Flow
```
1. BLE Scan & Discovery
2. Connect to GATT Server
3. Discover Services
4. Discover Characteristics
5. Enable Notifications (write 0x01 to CCCD)
6. Send Handshake Command
7. Ready for Communication
```

---

## Command Format

### Standard Command (5 bytes)
```
+--------+--------+--------+--------+----------+
| Byte 0 | Byte 1 | Byte 2 | Byte 3 | Byte 4   |
+--------+--------+--------+--------+----------+
| START  | LENGTH | TYPE   | DATA   | CHECKSUM |
|  0xAB  |  0x02  | 0x0C   |  VAR   |   VAR    |
+--------+--------+--------+--------+----------+
```

### Handshake Command (4 bytes)
```
+--------+--------+--------+--------+
| Byte 0 | Byte 1 | Byte 2 | Byte 3 |
+--------+--------+--------+--------+
| START  | TYPE   |  DATA  | END    |
|  0xAB  |  0x01  |  0xFF  | 0xAB   |
+--------+--------+--------+--------+
```

### Field Descriptions

#### START (Byte 0)
- **Value:** 0xAB (-85 as signed byte)
- **Purpose:** Packet start delimiter
- **Required:** Yes

#### LENGTH (Byte 1)
- **Value:** 0x02 (for standard commands)
- **Purpose:** Length indicator
- **Note:** May represent data length or packet type

#### TYPE (Byte 2)
- **0x01:** Handshake/special
- **0x0C:** Button command
- **0x12:** Acknowledgment
- **Purpose:** Categorizes the command

#### DATA (Byte 3)
- **Range:** 0x00 - 0xFF
- **Purpose:** Specific command code
- **See:** Command reference table below

#### CHECKSUM (Byte 4)
- **Calculation:** (Byte0 + Byte1 + Byte2 + Byte3) & 0xFF
- **Purpose:** Data integrity verification
- **Example:** For {0xAB, 0x02, 0x0C, 0x12}: (171+2+12+18) & 255 = 203 = 0xCB

---

## Command Reference

### Complete Command Table

| Category | Command | Data Code | Full Packet | Description |
|----------|---------|-----------|-------------|-------------|
| **System** ||||
| Power | Power Toggle | 0x14 | AB 02 0C 14 CD | Toggle power on/off |
| Power | Power Off Long | 0x45 | AB 02 0C 45 FE | Power off (long press) |
| BT | Bluetooth | 0x1C | AB 02 0C 1C D5 | Toggle Bluetooth mode |
| Handshake | Handshake | - | AB 01 FF AB | Initial handshake |
| **Numbers** ||||
| 0 | Number Zero | 0x0A | AB 02 0C 0A C3 | Press 0 |
| 1 | Number One | 0x01 | AB 02 0C 01 BA | Press 1 |
| 2 | Number Two | 0x02 | AB 02 0C 02 BB | Press 2 |
| 3 | Number Three | 0x03 | AB 02 0C 03 BC | Press 3 |
| 4 | Number Four | 0x04 | AB 02 0C 04 BD | Press 4 |
| 5 | Number Five | 0x05 | AB 02 0C 05 BE | Press 5 |
| 6 | Number Six | 0x06 | AB 02 0C 06 BF | Press 6 |
| 7 | Number Seven | 0x07 | AB 02 0C 07 C0 | Press 7 |
| 8 | Number Eight | 0x08 | AB 02 0C 08 C1 | Press 8 |
| 9 | Number Nine | 0x09 | AB 02 0C 09 C2 | Press 9 |
| **Navigation** ||||
| Nav | Back | 0x0B | AB 02 0C 0B C4 | Back/Delete |
| Nav | Point | 0x0C | AB 02 0C 0C C5 | Decimal point |
| Nav | Frequency | 0x0D | AB 02 0C 0D C6 | Frequency mode |
| Nav | Up Short | 0x0E | AB 02 0C 0E C7 | Frequency up |
| Nav | Up Long | 0x0F | AB 02 0C 0F C8 | Fast scan up |
| Nav | Down Short | 0x10 | AB 02 0C 10 C9 | Frequency down |
| Nav | Down Long | 0x11 | AB 02 0C 11 CA | Fast scan down |
| **Volume** ||||
| Vol | Volume Up | 0x12 | AB 02 0C 12 CB | Increase volume |
| Vol | Volume Down | 0x13 | AB 02 0C 13 CC | Decrease volume |
| **Band** ||||
| Band | Band | 0x00 | AB 02 0C 00 B9 | Band select |
| Band | Sub-Band | 0x17 | AB 02 0C 17 D0 | Sub-band |
| Band | Band Long | 0x29 | AB 02 0C 29 E2 | Band long press |
| **Audio** ||||
| Audio | Music | 0x26 | AB 02 0C 26 DF | Music mode |
| Audio | Play | 0x1A | AB 02 0C 1A D3 | Play/Pause |
| Audio | Play Long | 0x33 | AB 02 0C 33 EC | Play long press |
| Audio | Step | 0x1B | AB 02 0C 1B D4 | Next track |
| Audio | Circle | 0x27 | AB 02 0C 27 E0 | Loop mode |
| **Radio** ||||
| Radio | Demodulation | 0x1D | AB 02 0C 1D D6 | Change modulation |
| Radio | Bandwidth | 0x1E | AB 02 0C 1E D7 | Adjust bandwidth |
| Radio | Squelch | 0x20 | AB 02 0C 20 D9 | Squelch setting |
| Radio | Stereo | 0x21 | AB 02 0C 21 DA | Stereo toggle |
| Radio | DE | 0x22 | AB 02 0C 22 DB | De-emphasis |
| **Memory** ||||
| Mem | Preset | 0x23 | AB 02 0C 23 DC | Preset access |
| Mem | Memo | 0x24 | AB 02 0C 24 DD | Memory access |
| Mem | Memo Long | 0x2C | AB 02 0C 2C E5 | Save to memory |
| Mem | REC | 0x25 | AB 02 0C 25 DE | Recording |
| **Emergency** ||||
| SOS | SOS | 0x2A | AB 02 0C 2A E3 | SOS activate |
| SOS | SOS Long | 0x2B | AB 02 0C 2B E4 | SOS long press |
| Alarm | Alarm | 0x31 | AB 02 0C 31 EA | Alarm activate |
| Alarm | Alarm Long | 0x32 | AB 02 0C 32 EB | Alarm long press |

### Number Long Press Commands

| Number | Data Code | Full Packet | Description |
|--------|-----------|-------------|-------------|
| 1 Long | 0x35 | AB 02 0C 35 EE | Long press 1 |
| 2 Long | 0x36 | AB 02 0C 36 EF | Long press 2 |
| 3 Long | 0x37 | AB 02 0C 37 F0 | Long press 3 |
| 4 Long | 0x38 | AB 02 0C 38 F1 | Long press 4 |
| 5 Long | 0x39 | AB 02 0C 39 F2 | Long press 5 |
| 6 Long | 0x3A | AB 02 0C 3A F3 | Long press 6 |
| 7 Long | 0x3B | AB 02 0C 3B F4 | Long press 7 |
| 8 Long | 0x3C | AB 02 0C 3C F5 | Long press 8 |
| 9 Long | 0x3D | AB 02 0C 3D F6 | Long press 9 |
| 0 Long | 0x3E | AB 02 0C 3E F7 | Long press 0 |

---

## Response Format

### Response Packet Structure
```
+--------+--------+--------+--------+--------+-----+
| Byte 0 | Byte 1 | Byte 2 | Byte 3 | Byte 4 | ... |
+--------+--------+--------+--------+--------+-----+
| START  | LENGTH | CMD_ID | DATA.................|
|  0xAB  |  VAR   |  VAR   | VARIABLE LENGTH      |
+--------+--------+--------+--------+--------+-----+
```

### Response Types

#### Frequency Status (ab0417)
```
Offset  | Field          | Description
--------|----------------|---------------------------
0-1     | Header         | AB 04
2-3     | Command        | 17 (status update)
4-5     | Status Byte 1  | Flags/settings
6-7     | Status Byte 2  | Flags/settings  
8-9     | Status Byte 3  | Flags/settings
10-19   | Frequency      | 4-byte frequency data
20+     | Additional     | Extended status
```

#### Band Info (ab0901)
```
Offset  | Field          | Description
--------|----------------|---------------------------
0-1     | Header         | AB 09
2-3     | Command        | 01 (band info)
4-5     | Band Code      | Current band identifier
6-7     | Sub-Band 1     | Sub-band data
8-9     | Sub-Band 2     | Sub-band data
10-11   | Sub-Band 3     | Sub-band data
12-13   | Sub-Band 4     | Sub-band data
14-31   | Frequency      | 8 bytes frequency components
```

#### Volume Level (ab0303)
```
Offset  | Field          | Description
--------|----------------|---------------------------
0-1     | Header         | AB 03
2-3     | Command        | 03 (volume)
4-5     | Volume         | Volume level (0-15)
```

#### Time Update (ab031e)
```
Offset  | Field          | Description
--------|----------------|---------------------------
0-1     | Header         | AB 03
2-3     | Command        | 1E (time)
4+      | Time Data      | Clock/time information
```

---

## Data Types

### Frequency Encoding
- **Format:** 32-bit unsigned integer (little-endian)
- **Unit:** Hertz (Hz)
- **Range:** Varies by band
- **Example:** 145.500 MHz = 145,500,000 Hz = 0x08ADE940

### Band Codes
```
Code | Band Description
-----|------------------
00   | Unknown/Default
01   | VHF Low
02   | VHF High  
03   | UHF Low
04   | UHF High
06   | Special band (observed in code)
```

### Status Flags
Status information is packed into bytes using bit fields:
```
Bit 7 | Bit 6 | Bit 5 | Bit 4 | Bit 3 | Bit 2 | Bit 1 | Bit 0
------|-------|-------|-------|-------|-------|-------|-------
      |       |       | Vol H | Vol L | Vol L | Vol L | Vol L
```

Volume (bits 0-3): 0-15 (4 bits)
Squelch (bits 4-7): 0-15 (4 bits)

---

## State Machine

### Connection States
```
DISCONNECTED → CONNECTING → CONNECTED → DISCOVERING → READY
     ↑                                                    ↓
     └────────────────── disconnect() ──────────────────┘
```

### Command Flow
```
[App] → Send Command → [BLE Stack] → [Radio]
                              ↓
                         Characteristic
                           Write
                              ↓
                         [Radio Processes]
                              ↓
                         Notification ← [Radio]
                              ↓
[App] ← Parse Response ← [BLE Stack]
```

### Timing Considerations
- **Command Interval:** Minimum 50-100ms between commands
- **Vibration Feedback:** 100ms haptic pulse
- **Response Timeout:** 1-2 seconds for status updates
- **Connection Timeout:** 30 seconds for initial connection

---

## Error Handling

### Checksum Verification
```java
boolean verifyChecksum(byte[] packet) {
    if (packet.length < 5) return false;
    
    int sum = 0;
    for (int i = 0; i < packet.length - 1; i++) {
        sum += (packet[i] & 0xFF);
    }
    
    byte expectedChecksum = (byte)(sum & 0xFF);
    return expectedChecksum == packet[packet.length - 1];
}
```

### Common Errors
- **Not Ready:** Device not in READY state
- **Null Characteristic:** GATT characteristics not discovered
- **Write Failed:** Bluetooth write operation failed
- **Invalid Checksum:** Received packet checksum mismatch
- **Timeout:** No response within expected timeframe

---

## Implementation Examples

### Example 1: Set Frequency to 145.500 MHz
```java
// Press: 1, 4, 5, ., 5, 0, 0
sender.pressNumber(1);    // AB 02 0C 01 BA
Thread.sleep(100);
sender.pressNumber(4);    // AB 02 0C 04 BD
Thread.sleep(100);
sender.pressNumber(5);    // AB 02 0C 05 BE
Thread.sleep(100);
sender.pressPoint();      // AB 02 0C 0C C5
Thread.sleep(100);
sender.pressNumber(5);    // AB 02 0C 05 BE
Thread.sleep(100);
sender.pressNumber(0);    // AB 02 0C 0A C3
Thread.sleep(100);
sender.pressNumber(0);    // AB 02 0C 0A C3
```

### Example 2: Scan Up Through Band
```java
// Fast scan upward
sender.frequencyUpFast();  // AB 02 0C 0F C8 (hold)

// Stop at desired frequency
sender.pressBack();        // AB 02 0C 0B C4 (or release)
```

### Example 3: Save to Memory
```java
// Tune to desired frequency first...
// Then long-press memo
sender.saveMemo();         // AB 02 0C 2C E5
```

---

## Testing & Validation

### Unit Testing
- Verify checksum calculation
- Test command packet generation
- Validate hex/decimal conversions
- Check nibble extraction

### Integration Testing
- GATT connection establishment
- Service/characteristic discovery
- Notification enabling
- Command transmission
- Response parsing

### Hardware Testing
- Connect to actual radio device
- Verify each command function
- Monitor battery/power consumption
- Test range and reliability
- Validate all button mappings

---

## Appendix

### Hex to Decimal Conversion
```java
String hexToDec(String hex) {
    return String.valueOf(Long.parseLong(hex, 16));
}
```

### Decimal to Hex Conversion
```java
String decToHex(long value) {
    return String.format("%08X", value);
}
```

### Byte Array to Hex String
```java
String bytesToHex(byte[] bytes) {
    StringBuilder sb = new StringBuilder();
    for (byte b : bytes) {
        sb.append(String.format("%02X", b & 0xFF));
    }
    return sb.toString();
}
```

---

## Version History
- **v1.0** - Initial protocol documentation from reverse engineering
- Extracted from decompiled smali code (2025-10-24)

---

## References
- Bluetooth GATT Specification
- BLE Core Specification 4.0+
- Original APK: com.myhomesmartlife.bluetooth
- Decompiled using standard Android reverse engineering tools
