# Additional Message Types - Decoded from Wireshark

## Analysis of Long Messages

Based on the Wireshark captures, I've identified several additional message types that contain **ASCII text data**. These messages are multi-byte and carry human-readable information.

---

## Message Type: Device Version Info (AB11)

### Purpose
Multi-part message containing device version, model name, and contact information in ASCII format.

### Packet Structure
```
+------+--------+----------+-------------+------------------+----------+
| AB11 | LENGTH | SEQUENCE | DATA_LENGTH | ASCII_TEXT       | CHECKSUM |
+------+--------+----------+-------------+------------------+----------+
  0-1    2-3      4-5        6-7           8 to (8+length-1)  Last byte
```

### Fields
- **Header (AB11)**: Device info identifier
- **Length**: Total message length in bytes
- **Sequence**: Part number for multi-part messages (01, 02, 03, etc.)
- **Data Length**: Number of ASCII characters in this part
- **ASCII Text**: Human-readable device information
- **Checksum**: Message integrity check

### Example Decode

#### Message Part 1:
```
AB1119010E526164696F2076657273696F6E2019

Breakdown:
AB11 = Header (device info)
19   = Total length (25 bytes)
01   = Sequence #1 (first part)
0E   = Data length (14 bytes of text)
526164696F2076657273696F6E20 = "Radio version "
19   = Checksum

Text: "Radio version "
```

#### Message Part 2:
```
AB1119020E3A2056342E300A4D6F64656C203A7C

Breakdown:
AB11 = Header
19   = Length
02   = Sequence #2 (second part)
0E   = Data length (14 bytes)
3A2056342E300A4D6F64656C203A = ": V4.0\nModel :"
7C   = Checksum

Text: ": V4.0\nModel :"
```

#### Message Part 3:
```
AB1119020E2052616464792052463332300A0A5A

Breakdown:
02   = Sequence (continues)
0E   = Data length
2052616464792052463332300A0A = " Raddy RF320\n\n"
5A   = Checksum

Text: " Raddy RF320\n\n"
```

#### Message Part 4:
```
AB1119020E636F70796D61696C3A737570706FB4

02   = Sequence
0E   = Data length
636F70796D61696C3A737570706F = "copymail:suppo"
B4   = Checksum
```

#### Message Part 5:
```
AB1019040D7274406972616464792E636F6DF5

AB10 = Header (continuation or end marker)
19   = Length
04   = Sequence
0D   = Data length (13 bytes)
7274406972616464792E636F6D = "rt@iraddy.com"
F5   = Checksum
```

### Complete Assembled Message:
```
Radio version : V4.0
Model : Raddy RF320

Copymail:support@iraddy.com
```

### Notes:
- Messages are split into multiple parts due to BLE packet size limitations (typically 20 bytes)
- Sequence numbers increment for each part
- `\n` (0x0A) represents newline characters
- The device is a **Raddy RF320** running firmware **V4.0**

---

## Message Type: Sub-Band Information (AB0E)

### Purpose
Provides sub-band name or label in ASCII format.

### Packet Structure
```
+------+--------+-------+-------------+-----------+----------+
| AB0E | LENGTH | INDEX | DATA_MARKER | ASCII_NAME| CHECKSUM |
+------+--------+-------+-------------+-----------+----------+
  0-1    2-3      4-5     6-7          8+          Last
```

### Example Decode:
```
AB0E2101030A205355422042414E442047

Breakdown:
AB0E = Header (sub-band info)
21   = Length (33 bytes)
01   = Sub-band index #1
03   = Data section marker
0A   = Name length (10 characters)
205355422042414E4420 = " SUB BAND "
47   = Checksum

Text: " SUB BAND "
```

### Usage:
This message identifies available sub-bands within the current band. The radio likely has multiple sub-bands that can be selected.

---

## Message Type: Lock Status (AB08)

### Purpose
Indicates whether keypad/controls are locked.

### Packet Structure
```
+------+--------+-----------+-------------+-----------+----------+
| AB08 | LENGTH | LOCK_TYPE | DATA_MARKER | ASCII_TEXT| CHECKSUM |
+------+--------+-----------+-------------+-----------+----------+
  0-1    2-3      4-5         6-7          8+          Last
```

### Example Decode:
```
AB08210503044C4F434B09

Breakdown:
AB08 = Header (lock status)
21   = Length (33 bytes)
05   = Lock type/status code
03   = Data section marker
04   = Text length (4 characters)
4C4F434B = "LOCK"
09   = Checksum

Text: "LOCK"
```

### Interpretation:
- **"LOCK"**: Controls are locked (keypad disabled)
- Likely has corresponding **"UNLOCK"** message when unlocked

---

## Message Type: Recording Status (AB0B)

### Purpose
Shows current recording state.

### Packet Structure
```
+------+--------+--------+-------------+-----------+----------+
| AB0B | LENGTH | INDEX  | DATA_MARKER | ASCII_TEXT| CHECKSUM |
+------+--------+--------+-------------+-----------+----------+
  0-1    2-3      4-5      6-7          8+          Last
```

### Example Decode:
```
AB0B1C100307524543204F4646C1

Breakdown:
AB0B = Header (recording status)
1C   = Length (28 bytes)
10   = Recording slot/index (16 in decimal)
03   = Data section marker
07   = Text length (7 characters)
524543204F4646 = "REC OFF"
C1   = Checksum

Text: "REC OFF"
```

### Interpretation:
- **"REC OFF"**: Recording is disabled/stopped
- Index `10` (16) may indicate recording slot or channel
- Likely has **"REC ON"** or similar when recording is active

---

## ASCII Conversion Table (Quick Reference)

Common characters in these messages:

| Hex | Char | Hex | Char | Hex | Char | Hex | Char |
|-----|------|-----|------|-----|------|-----|------|
| 20  | (space) | 40 | @ | 61 | a | 4C | L |
| 0A  | \n (newline) | 56 | V | 64 | d | 4F | O |
| 3A  | : | 34 | 4 | 69 | i | 43 | C |
| 2E  | . | 30 | 0 | 6F | o | 4B | K |
| 52  | R | 4D | M | 72 | r | 46 | F |
| 46  | F | 42 | B | 73 | s | 45 | E |
| 33  | 3 | 41 | A | 65 | e | 4E | N |
| 32  | 2 | 4E | N | 6E | n | 44 | D |

---

## Summary of New Message Types

| Header | Type | Content | Example Text |
|--------|------|---------|--------------|
| AB11 | Device Info | Multi-part ASCII | "Radio version : V4.0..." |
| AB10 | Info Continuation | ASCII continuation | "...@iraddy.com" |
| AB0E | Sub-Band Info | Sub-band name | " SUB BAND " |
| AB08 | Lock Status | Lock state | "LOCK" |
| AB0B | Recording Status | Recording state | "REC OFF" |

---

## Integration Notes

### For RadioProtocolHandler.java

Add these constants:
```java
/** Device version/info packet (multi-part ASCII) */
public static final String CMD_TYPE_DEVICE_INFO = "ab11";

/** Device info continuation */
public static final String CMD_TYPE_DEVICE_INFO_CONT = "ab10";

/** Sub-band information packet (ASCII) */
public static final String CMD_TYPE_SUBBAND_INFO = "ab0e";

/** Lock status packet (ASCII) */
public static final String CMD_TYPE_LOCK_STATUS = "ab08";

/** Recording status packet (ASCII) */
public static final String CMD_TYPE_RECORDING_STATUS = "ab0b";
```

Add parsing methods:
```java
private StringBuilder deviceInfoBuffer = new StringBuilder();

private void parseDeviceInfo(String hexData) {
    if (hexData.length() < 14) return;
    
    try {
        int length = Integer.parseInt(hexData.substring(2, 4), 16);
        int sequence = Integer.parseInt(hexData.substring(4, 6), 16);
        int dataLength = Integer.parseInt(hexData.substring(6, 8), 16);
        
        // Extract ASCII text
        String textHex = hexData.substring(8, 8 + (dataLength * 2));
        String text = hexToAscii(textHex);
        
        // Accumulate multi-part message
        deviceInfoBuffer.append(text);
        
        Log.d(TAG, "Device info part " + sequence + ": " + text);
        
        // If this seems like the last part, process complete message
        if (text.contains("@") || text.contains(".com")) {
            String completeInfo = deviceInfoBuffer.toString();
            Log.i(TAG, "Complete device info: " + completeInfo);
            deviceInfoBuffer.setLength(0); // Clear for next message
        }
        
    } catch (Exception e) {
        Log.e(TAG, "Error parsing device info", e);
    }
}

private void parseLockStatus(String hexData) {
    if (hexData.length() < 16) return;
    
    try {
        int textLength = Integer.parseInt(hexData.substring(6, 8), 16);
        String textHex = hexData.substring(8, 8 + (textLength * 2));
        String status = hexToAscii(textHex);
        
        boolean isLocked = status.equals("LOCK");
        Log.d(TAG, "Lock status: " + status + " (locked=" + isLocked + ")");
        
    } catch (Exception e) {
        Log.e(TAG, "Error parsing lock status", e);
    }
}

private void parseRecordingStatus(String hexData) {
    if (hexData.length() < 16) return;
    
    try {
        int recordIndex = Integer.parseInt(hexData.substring(4, 6), 16);
        int textLength = Integer.parseInt(hexData.substring(6, 8), 16);
        String textHex = hexData.substring(8, 8 + (textLength * 2));
        String status = hexToAscii(textHex);
        
        boolean isRecording = !status.contains("OFF");
        Log.d(TAG, "Recording " + recordIndex + ": " + status + 
                   " (active=" + isRecording + ")");
        
    } catch (Exception e) {
        Log.e(TAG, "Error parsing recording status", e);
    }
}

/**
 * Convert hex string to ASCII text
 */
private String hexToAscii(String hexString) {
    StringBuilder output = new StringBuilder();
    for (int i = 0; i < hexString.length(); i += 2) {
        String str = hexString.substring(i, i + 2);
        output.append((char) Integer.parseInt(str, 16));
    }
    return output.toString();
}
```

Update the main parse method:
```java
switch (commandId) {
    case CMD_TYPE_DEVICE_INFO:
    case CMD_TYPE_DEVICE_INFO_CONT:
        parseDeviceInfo(hexString);
        break;
        
    case CMD_TYPE_SUBBAND_INFO:
        parseSubBandInfo(hexString);
        break;
        
    case CMD_TYPE_LOCK_STATUS:
        parseLockStatus(hexString);
        break;
        
    case CMD_TYPE_RECORDING_STATUS:
        parseRecordingStatus(hexString);
        break;
    
    // ... existing cases ...
}
```

---

## Testing

To test these messages, monitor the radio during:
1. **Power on** - Should receive AB11 device info messages
2. **Lock/unlock keypad** - Should receive AB08 messages
3. **Start/stop recording** - Should receive AB0B messages
4. **Change sub-band** - Should receive AB0E messages

---

## Device Information Revealed

From the decoded messages:
- **Manufacturer**: iRaddy
- **Model**: Raddy RF320
- **Firmware**: V4.0
- **Support Email**: support@iraddy.com

This is a **Raddy RF320** portable radio with recording capabilities, sub-band selection, and keypad lock features.

---

## Additional Packet Types from Extended Wireshark Capture

### Packet Type: AB02 - Status Short
**Format:** `AB02 [LENGTH] [STATUS] [CHECKSUM]`

**Examples:**
- `AB022001CE` → Status = 0x20
- `AB020500B2` → Status = 0x05
- `AB020702B6` → Status = 0x07 (may indicate battery update)

**Purpose:** Quick status/mode indicator. Observed during state transitions.

---

### Packet Type: AB05 + AB06 - Paired Frequency/Channel Data
These packets always appear as **paired messages** - AB05 followed immediately by AB06.

#### AB05 - Frequency Data Part 1
**Format:** `AB05 [LENGTH] [IDX1] [IDX2] [MODE] [CHECKSUM]`

**Examples:**
- `AB051C0603013107` → Index=0x1C06, Mode=0x31
- `AB051C0603013208` → Index=0x1C06, Mode=0x32
- `AB051C0603013309` → Index=0x1C06, Mode=0x33

**Purpose:** Contains frequency/channel index and mode selection.

#### AB06 - Frequency Data Part 2 (ASCII)
**Format:** `AB06 [LENGTH] [IDX1] [IDX2] [TEXT_LEN] [ASCII_TEXT...] [CHECKSUM]`

**Examples:**
- `AB061C08030233313E` → "31" (Channel 31)
- `AB061C08030233323F` → "32" (Channel 32)
- `AB061C08030233303D` → "30" (Channel 30)
- `AB061C080302323945` → "29" (Channel 29)

**Purpose:** Display text for channel/frequency. Shows what should appear on screen.

**Decoding AB06 "31" Example:**
```
AB 06 1C 08 03 02 33 31 3E
│  │  │  │  │  │  │  │  └─ Checksum
│  │  │  │  │  │  └──└──── ASCII: 0x33='3', 0x31='1' → "31"
│  │  │  │  │  └─────────── Text length: 0x02 (2 chars)
│  │  │  │  └────────────── Unknown marker: 0x03
│  │  └──└─────────────────── Index: 0x1C08
│  └────────────────────────── Length: 0x06
└───────────────────────────── Header: AB06
```

---

### Packet Type: AB07 - Battery Status
**Format:** `AB07 [LENGTH] [STATUS] [BATTERY] [CHECKSUM]`

**Examples:**
- `AB020702B6` → Battery value = 0x02 (low battery warning?)

**Purpose:** Battery level indicator. May also contain extended status info.

**Battery Interpretation:** Raw values 0x00-0x07 may represent battery levels (needs calibration with actual device).

---

### Packet Type: AB09 - Detailed Frequency Info
**Format:** `AB09 [LENGTH] [INDEX] [MODE] [FREQ_DATA...] [PARAM] [CHECKSUM]`

**Examples:**
- `AB090106927A0200001300DC`
  - Index: 0x01
  - Mode: 0x06
  - Frequency: 927A020000 (6 bytes)
  - Parameter: 0x13 (possibly squelch level)

**Purpose:** Extended frequency information with demodulation settings and additional parameters.

---

### Packet Type: AB0D - Bandwidth Info (ASCII)
**Format:** `AB0D [LENGTH] [IDX1] [IDX2] [TEXT_LEN] [ASCII_TEXT...] [CHECKSUM]`

**Examples:**
- `AB0D1C03030942616E64576964746858` → "BandWidth"

**Decoding:**
```
Hex: 42 61 6E 64 57 69 64 74 68
     B  a  n  d  W  i  d  t  h
```

**Purpose:** Displays bandwidth setting text. Part of detailed radio configuration display.

---

### Packet Type: AB10 - Device Info Extension (ASCII)
**Format:** `AB10 [LENGTH] [SEQ] [TEXT_LEN] [ASCII_TEXT...] [CHECKSUM]`

**Examples from multi-part messages:**
- `AB101C01030C44656D6F64756C6174696F6ECC` → "Demodulation"
- `AB1019040D7274406972616464792E636F6DF5` → "rt@iraddy.com" (end of device info)

**Purpose:** Continuation/extension of AB11 device info messages. Also used for configuration labels.

---

### Packet Type: AB11 - Extended Device Info (ASCII)
**Format:** `AB11 [LENGTH] [SEQ] [TEXT_LEN] [ASCII_TEXT...] [CHECKSUM]`

**Examples (beyond initial device info):**
- `AB111C0B010D4D6F64656CA3BA52616464792053` → "Model�:Raddy S"
- `AB091C0B0405524633323011` → "RF320"

**Purpose:** Multi-part device information and configuration labels. Sequence number indicates message part.

---

## Summary of All Packet Types

| Header | Type | Content | Paired? | Example |
|--------|------|---------|---------|---------|
| **AB02** | Status Short | Status byte | No | AB022001CE |
| **AB03** | Volume/Signal | Volume/signal level | No | AB03080300B9 |
| **AB04** | Frequency Status | Main frequency data | No | AB0417010101C9 |
| **AB05** | Freq Data 1 | Channel index/mode | Yes (→AB06) | AB051C0603013107 |
| **AB06** | Freq Data 2 | Channel text (ASCII) | Yes (←AB05) | AB061C08030233313E → "31" |
| **AB07** | Battery | Battery level | No | AB020702B6 |
| **AB08** | Lock Status | Lock state (ASCII) | No | AB08...04LOCK |
| **AB09** | Detailed Freq | Extended freq info | No | AB090106927A02... |
| **AB0B** | Recording | Recording state (ASCII) | No | AB0B...REC OFF |
| **AB0D** | Bandwidth | Bandwidth text (ASCII) | No | AB0D...BandWidth |
| **AB0E** | Sub-Band | Sub-band name (ASCII) | No | AB0E...SUB BAND |
| **AB10** | Info Extension | Config labels (ASCII) | No | AB10...Demodulation |
| **AB11** | Device Info | Device details (ASCII) | No | AB11...Radio version |

---

## Implementation Status

All packet types have been **implemented in RadioProtocolHandler.java**:

✅ **AB02** - `parseStatusShort()` - Status monitoring
✅ **AB05** - `parseFreqData1()` - Frequency index buffering
✅ **AB06** - `parseFreqData2()` - Channel display text with listener callback
✅ **AB07** - `parseBattery()` - Battery level with listener callback
✅ **AB08** - `parseLockStatus()` - Lock state monitoring
✅ **AB09** - `parseDetailedFreq()` - Extended frequency parsing
✅ **AB0B** - `parseRecordingStatus()` - Recording state monitoring
✅ **AB0D** - `parseBandwidth()` - Bandwidth display text
✅ **AB0E** - `parseSubBandInfo()` - Sub-band name display
✅ **AB10** - `parseDeviceInfo()` - Configuration labels
✅ **AB11** - `parseDeviceInfo()` - Multi-part device info assembly

### New Listener Callbacks Added:
```java
void onBatteryLevel(int batteryPercent);
void onChannelDisplay(String channelText);
```

### Paired Message Handling:
The AB05/AB06 paired message sequence is automatically handled:
1. AB05 received → Data buffered in `lastFreqData1`
2. AB06 received → Combined with buffered AB05, callback fired, buffer cleared
