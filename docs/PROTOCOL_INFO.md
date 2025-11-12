# Radio-C Bluetooth Protocol (Reverse-Engineered)

> Source: Decompiled Android app `MainActivity.smali` & companions in `com/myhomesmartlife/bluetooth`.
> Language mix: English + Mandarin (Chinese) logs. Chinese phrases provide hints (e.g. “数据接收了哦” = "data received", “频率” = "frequency").
>
> Target reimplementation language: C#.

## 1. Device Discovery & Selection

- Uses `BluetoothLeScanner.startScan(ScanCallback)` with inner class `MainActivity$1`.
- `onScanResult` logs device name (tag `devicename====>`), then passes the `BluetoothDevice` to `DeviceDialog.setData()` for user selection.
- No code-level filtering (no name/MAC/UUID pre-filter) – selection is user driven.
- Scan stopped in `MainActivity.onConnect()` before initiating GATT connection.

## 2. GATT Connection & Service/Characteristic Mapping

1. User selects device → `onConnect(BluetoothDevice)`:
   - Stops ongoing scan.
   - Disconnects any existing GATT if state==2.
   - Calls `device.connectGatt(context, true, new MainActivity$162())`. (autoConnect = true)
2. `BluetoothGattCallback` (`MainActivity$162`):
   - `onConnectionStateChange`: when state == `CONNECTED (2)` → `discoverServices()`; on `DISCONNECTED (0)` closes GATT.
   - `onServicesDiscovered`: iterates all services & characteristics and matches UUID strings.
     - Write (TX) characteristic: `0000ff13-0000-1000-8000-00805f9b34fb` stored in fields `j` (service) & `i` (characteristic).
     - Notify (RX) characteristic: `0000ff14-0000-1000-8000-00805f9b34fb` stored in `l` & `m`.
   - After mapping: sends handshake frame (`SendData.sendWoshou`) via `dataSend([B)` and then invokes `MainActivity.a(gatt, O(characteristic))` (enables notifications – likely writes Client Characteristic Configuration descriptor 0x2902).
3. Writing Path: `dataSend` / `dataSendnew` set value on characteristic `i` (UUID ff13) and call `BluetoothGatt.writeCharacteristic`.
4. Notification Path: `onCharacteristicChanged` → forwards raw bytes to private parser `MainActivity.a([B)`.

### UUID Semantics
Vendor-specific 16-bit short UUIDs in the Bluetooth base UUID (0xFF13 / 0xFF14). Conventionally 0xFFxx are custom services/characteristics. Roles:
- FF13: Command / outbound write channel.
- FF14: State / inbound notify channel.

## 3. Frame Structures (Outbound)

Two principal frame formats discovered:

1. Handshake Frame (`sendWoshou`, 4 bytes):
   - Bytes (hex): `AB 01 FF AB`
   - Pattern: Header 0xAB, type 0x01, payload 0xFF, trailing 0xAB (mirrors header). Initiates session; expect an acceptance response.
2. Standard Command Frame (mostly button & long-click actions), 5 bytes:
   - Layout: `[0] Header 0xAB, [1] 0x02, [2] Group (0x0C for button, 0x12 for acknowledge), [3] CommandId, [4] Check/Complement`
   - Check Byte Rule:
     - For Group 0x0C: `check = 0xB9 + CommandId (mod 256)`
     - For Group 0x12: `check = 0xBF + CommandId (mod 256)`
   - All button presses are group 0x0C.
   - Acceptance / result frames (`accectSuccess`, `accectFaile`) are group 0x12 with CommandId 0x01 (success) or 0x00 (fail).

### Example Calculations
- `sendButtonFreq`: bytes from array_e → `AB 02 0C 0D C6` (since 0xB9 + 0x0D = 0xC6).
- `sendButtonPower`: `AB 02 0C 14 CC` (0xB9 + 0x14 = 0xCD? Wait actual last is -0x33 = 0xCD; confirm: header listing shows -0x33 → 0xCD. Earlier inference said 0xCC; corrected: base=0xB9, 0xB9+0x14=0xCD). Correction integrated below.

### Command Mapping Table (Group 0x0C)
> All frames start with `AB 02 0C` then ID then CheckByte.

| Constant Name | CommandId (hex) | Full Frame (hex) | Notes |
|---------------|-----------------|------------------|-------|
| sendBand | 00 | AB 02 0C 00 B9 | Band (?)
| sendNumberOne | 01 | AB 02 0C 01 BA | Numeric entry
| sendNumberTwo | 02 | AB 02 0C 02 BB | ...
| sendNumberThree | 03 | AB 02 0C 03 BC | 
| sendNumberFour | 04 | AB 02 0C 04 BD | 
| sendNumberFive | 05 | AB 02 0C 05 BE | 
| sendNumberSix | 06 | AB 02 0C 06 BF | 
| sendNumberSeven | 07 | AB 02 0C 07 C0 | 
| sendNumberEight | 08 | AB 02 0C 08 C1 | 
| sendNumberNine | 09 | AB 02 0C 09 C2 | 
| sendNumberZero | 0A | AB 02 0C 0A C3 | 
| sendButtonBack | 0B | AB 02 0C 0B C4 | Delete/backspace
| sendButtonPoint | 0C | AB 02 0C 0C C5 | Decimal point
| sendButtonFreq | 0D | AB 02 0C 0D C6 | Enter frequency mode / confirm
| sendButtonTopShortClick | 0E | AB 02 0C 0E C7 | Short up
| sendButtonTopLongtClick | 0F | AB 02 0C 0F C8 | Long up
| sendButtonDownShortClick | 10 | AB 02 0C 10 C9 | Short down
| sendButtonDownLongClick | 11 | AB 02 0C 11 CA | Long down
| sendButtonVolAdd | 12 | AB 02 0C 12 CB | Volume +
| sendButtonVolDel | 13 | AB 02 0C 13 CC | Volume -
| sendButtonPower | 14 | AB 02 0C 14 CD | Power toggle
| sendButtonSubBand | 17 | AB 02 0C 17 D0 | Sub band
| sendButtonMusic | 26 | AB 02 0C 26 DA | Music mode
| sendButtonPlay | 1A | AB 02 0C 1A D3 | Play/Pause
| sendButtonLongPlay | 33 | AB 02 0C 33 EC | Long play (hold)
| sendButtonStep | 1B | AB 02 0C 1B D4 | Step size change
| sendButtonCircle | 27 | AB 02 0C 27 DB | Cycle mode
| sendTypeMusicCircle | 28 | AB 02 0C 28 DC | Music cycle type
| sendButtonBluetooth | 1C | AB 02 0C 1C D5 | Bluetooth function
| sendButtonDemodulation | 1D | AB 02 0C 1D D6 | Demodulation
| sendButtonBandWidth | 1E | AB 02 0C 1E D7 | Bandwidth toggle
| sendButtonMobileDisPlay | 1F | AB 02 0C 1F D8 | Mobile display on/off
| sendButtonSQ | 20 | AB 02 0C 20 D9 | Squelch
| sendButtonStereo | 21 | AB 02 0C 21 DA | Stereo toggle (NOTE: collision with Music job; names reused - verify)
| sendButtonDe | 22 | AB 02 0C 22 DB | De-emphasis/EQ
| sendButtonPreset | 23 | AB 02 0C 23 DC | Preset save/recall
| sendButtonMemo | 24 | AB 02 0C 24 DD | Memo
| sendButtonRec | 25 | AB 02 0C 25 DE | Record
| sendButtonBandLong | 29 | AB 02 0C 29 E2 | Long band press
| sendButtonDem | 1D | (Duplicate constant → same as demodulation) | Obfuscation duplication
| sendSOSclick | 2A | AB 02 0C 2A E3 | SOS short
| sendSOSLongclick | 2B | AB 02 0C 2B E4 | SOS long
| sendRECclick | 2D | AB 02 0C 2D E6 | Record click (distinct from sendButtonRec?)
| sendMEMOLongclick | 2C | AB 02 0C 2C E5 | Memo long
| sendStepNewclick | 2E | AB 02 0C 2E E7 | Alternate step
| firstClick | 1D | (Alias) | Reused IDs – UI variants
| twoClick | 1E | (Alias) | 
| thirdClick | 2E | (Alias) | 
| fourClick | 2F | AB 02 0C 2F E8 | 
| fiveClick | 30 | AB 02 0C 30 E9 | 
| alarmDadunClick | 31 | AB 02 0C 31 EA | 
| alarmDadunLongClick | 32 | AB 02 0C 32 EB | 
| funcLongClick | 34 | AB 02 0C 34 ED | Function long
| Long-click numerics & specials | 35–49 | AB 02 0C <ID> (B9+ID) | Extensive set for hold behavior

> NOTE: Some constants share the same CommandId (obfuscation / multiple UI paths). Verify collisions empirically when implementing.

### Acknowledge Frames (Group 0x12)
| Constant | CommandId | Frame | Meaning |
|----------|-----------|-------|---------|
| accectSuccess | 01 | AB 02 12 01 C0 | Positive acknowledgement (e.g., handshake OK)
| accectFaile | 00 | AB 02 12 00 BF | Negative acknowledgement

## 4. Incoming Frame Parsing (`MainActivity.a([B)`) Overview

Incoming notification value converted to a hex string (`bytesToHexString`). Parser extracts leading 6 hex chars (`substring(0,6)`) which act as a signature (header + group + subcommand). Recognized signatures include:

| Signature | Purpose | Actions |
|-----------|---------|---------|
| `ab0417` | Frequency components (three subsequent 2-byte hex segments) | Spawns runnable `$163` updating UI with parsed pieces.
| `ab031e` / `ab031f` / `ab0303` | Likely smaller status / maybe mode or function state | UI updates via runnables `$164`, `$173`, `$174`.
| `ab0901` | Full state & frequency snapshot (Mode 1) | Parses multiple segments into fields: `at, ax, ay, az, aA, cj, ch` + unit and frequency digits; triggers UI updates & Handler message 0x3f2.
| `ab090f` | Full state & frequency snapshot (Alternate / Input Mode) | Similar parsing path with offsets adjusted; updates frequency `K` and related nibble fields `af`, `ag`.

### Frequency Assembly
- Frequency digits extracted as concatenated substrings: building string `ch` (or `K`) from segments `v6,v5,v11,v1` (indices depend on signature variant).
- `hexToDec(ch)` returns decimal frequency string; nibble extraction from the first byte splits low/high nibble into `af` and `ag` (likely used for UI icons showing modulation or band).
- Unit selection:
  - For `ab0901`: substring at offsets (hex bytes 0x10–0x12) yields `00` → MHZ (`$170` runnable), `01` → KHZ (`$171`).
  - For `ab090f`: different offset (substring at bytes group parsed into `v3`) with same comparison launching `$177` (MHZ) or `$178` (KHZ).

### Handler Interaction
- When frequency/state parsed and conditions not special-case (e.g., not equal to `000000ff` with `at == 06`), posts `Message.what = 0x3F2` to Handler `$160`.
- Handler 0x3F2: clears animation and calls UI update method `a(MainActivity; af, ag, K)` (multi-field UI refresh).
- Handler 0x3EA (unused here) triggers Picasso image load – possibly visual feedback event.

### State Fields (names inferred from obfuscated fields)
| Field | Origin (Parser) | Likely Meaning |
|-------|-----------------|----------------|
| at | First mode/state byte after signature (e.g., input mode indicator) |
| ax, ay, az, aA | Subsequent grouped bytes – may represent band, modulation, step, stereo, etc. |
| cj | Mode / context code (substring 0x14–0x16 or variant) |
| ch/K | Frequency hex string (after assembly, converted) |
| af, ag | Nibbles of first frequency byte – maybe high/low part used for display segmentation |

Full semantic meaning requires live observation; names like `频率` logs confirm frequency relevance.

## 5. Handshake & Acknowledgement
- After services discovered: App sends `sendWoshou` (`AB 01 FF AB`). Then enables notifications on RX characteristic.
- Expect an incoming ack frame (Group 0x12) with success (`AB 02 12 01 C0`). Failure indicated by (`AB 02 12 00 BF`). Parser branch for these signatures not shown in provided excerpts but message arrays define them; implement listener to catch group=0x12 frames early in session.

## 6. Polling / Periodicity
- No evidence of outgoing periodic polling (no `postDelayed`, `sendMessageDelayed` sending command frames).
- Frequency/state updates appear event-driven (likely device pushes them after changes or at intervals internally).
- Handler receives updates only when parser posts message based on inbound frames.

## 7. Checksum / Validation Strategy
- For Group 0x0C frames: `Check = 0xB9 + CommandId (mod 256)`.
- For Group 0x12 frames: `Check = 0xBF + CommandId (mod 256)`.
- Validation on receive: If header==0xAB & byte1==0x02 then: determine group=byte2, compute expected base (0xB9 or 0xBF) and verify `byte4 == base + byte3`.
- Handshake frame (length 4) differs; treat as special-case.

## 8. C# Reimplementation Guidance

### Data Structures
```csharp
public enum CommandGroup : byte {
    Button = 0x0C,
    Ack = 0x12
}

public enum CommandId : byte {
    Band = 0x00,
    Number1 = 0x01,
    Number2 = 0x02,
    // ... Continue for all mapped IDs ...
    Power = 0x14,
    SubBand = 0x17,
    Music = 0x26,
    LongPlay = 0x33,
    // Ack group ids:
    AckFail = 0x00,
    AckSuccess = 0x01,
}

public sealed class RadioFrame {
    public byte Header { get; set; } = 0xAB;
    public byte Proto { get; set; } = 0x02;
    public CommandGroup Group { get; set; }
    public byte CommandId { get; set; }
    public byte Check { get; set; }

    public byte[] ToBytes() {
        if (Header != 0xAB || Proto != 0x02)
            throw new InvalidOperationException("Invalid header/proto");
        byte baseVal = Group switch {
            CommandGroup.Button => 0xB9,
            CommandGroup.Ack => 0xBF,
            _ => throw new InvalidOperationException("Unsupported group")
        };
        Check = (byte)(baseVal + CommandId);
        return new[]{ Header, Proto, (byte)Group, CommandId, Check };
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out RadioFrame frame) {
        frame = null;
        if (data.Length == 5 && data[0] == 0xAB && data[1] == 0x02) {
            var group = (CommandGroup)data[2];
            byte baseVal = group switch {
                CommandGroup.Button => 0xB9,
                CommandGroup.Ack => 0xBF,
                _ => 0
            };
            if (baseVal == 0) return false;
            byte cmd = data[3];
            byte check = data[4];
            if (check != (byte)(baseVal + cmd)) return false;
            frame = new RadioFrame { CommandId = cmd, Group = group, Check = check };
            return true;
        }
        // Handshake
        if (data.Length == 4 && data[0] == 0xAB && data[1] == 0x01 && data[3] == 0xAB) {
            frame = new RadioFrame { Header = 0xAB, Proto = 0x01, Group = 0, CommandId = data[2], Check = data[3] };
            return true; // treat specially
        }
        return false;
    }
}
```

### Frequency Parsing (Notification Payload)
```csharp
public sealed class RadioState {
    public string RawHex { get; init; }
    public string FrequencyHex { get; init; }
    public double FrequencyMHz { get; init; } // normalized
    public bool UnitIsMHz { get; init; }
    public byte LowNibble { get; init; }
    public byte HighNibble { get; init; }
    // Additional fields: at, ax, ay, az, aA, cj etc.
}

public static RadioState ParseNotification(byte[] value) {
    string hex = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
    if (hex.Length < 12) return null;
    string signature = hex.Substring(0, 6); // abXXXX
    var state = new RadioState { RawHex = hex };
    switch (signature) {
        case "ab0901":
            // Offsets mirrored from smali logic.
            // Extract segments; adjust if live traces differ.
            string seg1 = hex.Substring(6, 2); // at
            string freqPart1 = hex.Substring(8, 2);
            string freqPart2 = hex.Substring(10, 2);
            string freqPart3 = hex.Substring(12, 2);
            string freqPart4 = hex.Substring(14, 2);
            state.FrequencyHex = freqPart1 + freqPart2 + freqPart3 + freqPart4;
            state.LowNibble = (byte)(Convert.ToByte(freqPart1, 16) & 0x0F);
            state.HighNibble = (byte)((Convert.ToByte(freqPart1, 16) >> 4) & 0x0F);
            state.FrequencyMHz = HexFrequencyToDouble(state.FrequencyHex);
            string unitByte = hex.Substring(16, 2); // '00' => MHz, '01' => KHz (per app logic)
            state.UnitIsMHz = unitByte == "00";
            break;
        case "ab090f":
            // Alternate layout; adapt similarly.
            // ...
            break;
        default:
            return null; // unknown signature
    }
    return state;
}

private static double HexFrequencyToDouble(string freqHex) {
    // Observed app uses hexToDec then decimal format 0.000.
    var raw = Convert.ToUInt32(freqHex, 16); // Treat hex digits as numeric scale
    // Heuristic: if raw is in kHz * 10? (Needs empirical confirmation)
    // Provide adaptable scaling:
    return raw / 1000.0; // placeholder scaling
}
```
> NOTE: Precise scaling requires observing real frequency examples; adjust divisor accordingly (e.g., /100, /10, /1e6). App formats with `DecimalFormat("0.000")` so expects a fractional MHz representation.

### Handshake & Session Init
```csharp
public async Task<bool> InitializeAsync(BluetoothLEDevice device) {
    // 1. Get services characteristics ff13 (write) & ff14 (notify)
    // 2. Enable notifications on ff14
    // 3. Send handshake
    var handshake = new byte[]{ 0xAB, 0x01, 0xFF, 0xAB };
    await WriteAsync(handshake);
    // 4. Await AckSuccess within timeout
    var ack = await WaitForFrameAsync(CommandGroup.Ack, CommandId.AckSuccess, TimeSpan.FromSeconds(3));
    return ack != null;
}
```

### Building Button Frames
```csharp
public byte[] BuildButton(CommandId id) {
    var frame = new RadioFrame { Group = CommandGroup.Button, CommandId = (byte)id };
    return frame.ToBytes();
}
```

### Validation Checklist
- Always verify Check byte formula before acting on inbound button/ack frames.
- Distinguish handshake (length 4) from command frames (length 5).
- Reject frames with mismatched base formula or unknown group.

## 9. UI Event Mapping
Every `OnClickListener` inner class (`MainActivity$XXX.smali`) loads the corresponding static byte array from `SendData` then calls `MainActivity.dataSend([B)`. Mapping examples:
| UI Listener Class | Frame Constant | Function (Inferred) |
|-------------------|----------------|---------------------|
| $119 | sendButtonFreq | Frequency confirm / entry mode |
| $123 | sendButtonPower | Power toggle |
| $117 | sendButtonVolAdd | Volume + |
| $139 | sendButtonVolDel | Volume - |
| $151 | sendButtonPlay | Play/Pause |
| $152 | sendButtonLongPlay | Long play (hold) |
| $161 | sendButtonBluetooth | Bluetooth mode |
| $137 | sendButtonDemodulation | Demodulation selection |
| $138 | sendButtonBandWidth | Bandwidth cycle |
| $140 | sendButtonMobileDisPlay | Mobile display toggle |
| $125 | sendButtonCircle | Cycle mode |
| $127/$129/$130 | Top/Down short/long clicks (navigation) |
| $2 | sendButtonPreset | Preset recall/save |
| $24 | sendButtonMemo | Memo |
| $46 | sendButtonMusic | Music mode |
| $116 | sendButtonRec | Record |
| Others ($13, $68 etc.) | Long numeric presses (sendNumerX...) |

## 10. State Snapshot Frames
- Signatures `ab0901` & `ab090f` deliver composite state including frequency digits, unit, several mode bytes, plus nibble-coded metadata.
- Special-case: if `at == "06"` and `ch == "000000ff"` triggers a distinct UI runnable `$168` (likely placeholder / scanning state) instead of normal Handler update.

## 11. Edge Cases & Reimplementation Notes
| Edge Case | Strategy |
|-----------|----------|
| Duplicate command IDs | Use first-known semantic; log collisions; consider mapping secondary names to same action. |
| Unknown signature | Ignore/log; maintain extensibility. |
| Handshake failure (accectFaile) | Retry connect or abort session. |
| Partial notifications | Discard; expect full hex string length (≥12) for frequency frames. |
| Scaling frequency | Empirically derive correct divisor by comparing displayed vs parsed raw value. |

## 12. Suggested Testing Flow (C#)
1. Discover devices, choose target.
2. Connect; enumerate services; locate characteristics. Assert both exist.
3. Enable notifications on ff14; subscribe.
4. Send handshake; await ack; assert success.
5. Issue sample button commands (Power, Vol+, Freq) and capture device responses.
6. Validate parser against captured notifications; adjust scaling.
7. Run automated tests for checksum building & parsing.

### Sample Unit Test Skeleton
```csharp
[TestMethod]
public void BuildPowerFrame_ChecksumMatches() {
    var bytes = BuildButton(CommandId.Power);
    Assert.AreEqual(5, bytes.Length);
    Assert.AreEqual(0xAB, bytes[0]);
    Assert.AreEqual(0x02, bytes[1]);
    Assert.AreEqual(0x0C, bytes[2]);
    Assert.AreEqual(0x14, bytes[3]);
    Assert.AreEqual((byte)(0xB9 + 0x14), bytes[4]); // 0xCD
}
```

## 13. Implementation Roadmap
1. BLE scaffolding (scan, select, connect).
2. Characteristic discovery + notification enabling.
3. Frame builder & validator.
4. Handshake & ack handling.
5. Button action API (high-level methods calling frame builder + write).
6. Parser for notification signatures and frequency state.
7. State model exposure to UI / application.
8. Robust logging + diagnostic hex dumps.
9. Empirical calibration of frequency scaling.
10. Extended commands (long-click numerics, SOS) as needed.

## 14. Glossary (Chinese Terms Observed)
| Term | Translation | Context |
|------|-------------|---------|
| 数据接收了哦 | Data received | `onCharacteristicChanged` log |
| 命令 | Command | Parser branching logs |
| 频率 | Frequency | Frequency string logs |
| 中中中 / 的命令 | Emphatic / command marker | Service discovery logs |

## 15. Open Questions (For Live Validation)
- Exact meaning of each state byte (at, ax, ay, az, aA, cj). Hypothesis: band, step size, modulation, stereo, squelch code.
- Correct frequency scaling factor (hex to MHz). Requires observing real device values.
- Accept frame timing (immediately after handshake or after certain command?).
 - Meaning of rapid repeating signatures (e.g., `ab061c`) seen in capture – likely periodic lightweight status/beacon; needs full frame collection beyond first 3 bytes.

## 15a. Long-Click / Special Function Mapping Dictionary
The app defines extended long-press frames with CommandIds 0x35–0x49 (Group 0x0C). All share the same checksum rule (0xB9 + ID). Names are partially transliterated from Chinese pinyin / shorthand:

| CommandId | Constant | Intended Meaning (Hypothesis) |
|-----------|----------|--------------------------------|
| 35 | sendNumerOnelongClick | Long press numeric 1 (preset/quick recall) |
| 36 | sendNumerTwolongClick | Long press numeric 2 |
| 37 | sendNumerThreelongClick | Long press numeric 3 |
| 38 | sendNumerFOURlongClick | Long press 4 |
| 39 | sendNumerFivelongClick | Long press 5 |
| 3A | sendNumerSixlongClick | Long press 6 |
| 3B | sendNumerSevenlongClick | Long press 7 |
| 3C | sendNumerEightlongClick | Long press 8 |
| 3D | sendNumerNinelongClick | Long press 9 |
| 3E | sendNumerTenlongClick | Long press 10 / 0 (ambiguous) |
| 3F | sendNumerMusiclongClick | Music mode secondary (cycle EQ?) |
| 40 | sendNumerPlaylongClick | Play hold (seek / resume) |
| 41 | sendNumerModelongClick | Mode hold (toggle modulation set) |
| 42 | sendNumerEqlongClick | Equalizer / DSP long press |
| 43 | sendNumerJianlongClick | "Jian" (减) decrement long press (coarse down) |
| 44 | sendNumerJialongClick | "Jia" (加) increment long press (coarse up) |
| 45 | sendNumerPowerlongClick | Power hold (power off / standby) |
| 46 | sendNumerEnterlongClick | Enter/confirm long press (store) |
| 47 | sendNumerDianlongClick | "Dian" (点) decimal point hold (clear fraction?) |
| 48 | sendNumerDeltelongClick | Delete hold (clear entry) |
| 49 | sendNumerMetelongClick | "Mete" / Meter / Memory (store current freq) |

Duplicate / alias collisions previously noted:
| CommandId | Aliases | Resolution Strategy |
|-----------|---------|---------------------|
| 1D | sendButtonDemodulation, sendButtonDem, firstClick | Use semantic `Demodulation`; treat others as UI variants. |
| 1E | twoClick | Keep primary `ButtonBandWidth` meaning for 0x1E. |
| 2E | sendStepNewclick, thirdClick | Treat both as alternate step function. |

Implementation: Provide a single dictionary mapping CommandId → CanonicalAction enum; log aliases when building frames from legacy names.

## 15b. Descriptor (0x2902) Enabling Notifications
After locating characteristic FF14 (notify), client must write Client Characteristic Configuration (CCC) descriptor UUID `00002902-0000-1000-8000-00805f9b34fb` with value:
- For notifications: `[0x01, 0x00]`
- For indications (not used here): `[0x02, 0x00]`

Pseudo steps (C# Windows.Devices.Bluetooth):
```csharp
var ccc = notifyChar.GetDescriptors(new Guid("00002902-0000-1000-8000-00805f9b34fb")).First();
await ccc.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(new byte[]{0x01,0x00}));
```
Android (for reference):
```java
gatt.setCharacteristicNotification(notifyChar, true);
BluetoothGattDescriptor ccc = notifyChar.getDescriptor(UUID.fromString("00002902-0000-1000-8000-00805f9b34fb"));
ccc.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);
gatt.writeDescriptor(ccc);
```
Failure handling: Retry once on GATT_STATUS_BUSY; if still failing, reconnect.

## 15c. Frequency Math Deep Dive
Observed capture (PROTOCOL_SEQUENCE_SAMPLE.txt) lists only signature prefixes (first 3 bytes shown as 6 hex chars). Full frequency frames `ab0901` appear multiple times (Frames 848, 874). Without full payload we cannot definitively scale; however:
1. Parser concatenates four subsequent byte pairs into a hex string, then converts hex → decimal string (`hexToDec`) and formats `0.000`. This suggests the raw hex is a BCD-like representation of MHz * 1000 or kHz.
2. Heuristic: If frequency 145.125 MHz were sent, plausible hex sequence might assemble to `09130695` (example) whose decimal could be scaled by /1000.
3. Pending live examples, implement adaptive scaling: Try /1000; if result > 2000 assume /100000; if < 10 assume /10.

Proposed adaptive method:
```csharp
double ScaleFrequency(uint raw){
    if (raw > 2_000_000) return raw / 100_000.0; // treat as kHz*100
    if (raw > 200_000) return raw / 1000.0;      // treat as kHz
    if (raw > 10_000) return raw / 100.0;        // treat as 10*kHz
    return raw / 10.0;                           // fallback
}
```
Log both raw and scaled; allow manual override.

## 15d. Capture Signature Interpretation (From PROTOCOL_SEQUENCE_SAMPLE.txt)
| Signature | Observed Count | Hypothesis |
|-----------|----------------|------------|
| AB061C | Frequent repeating | Heartbeat / minor status (state byte 0x1C) |
| AB01FF | Handshake echo? | Possibly device response mirroring `sendWoshou` minus trailer |
| AB0220 | Squelch state update (0x20) |
| AB0417 | Frequency component seed (matches parser branch) |
| AB0E21 / AB0821 | Stereo / Music sub-state toggles |
| AB0B1C | Back/Delete action acknowledgement |
| AB0205 | Numeric 0x05 press acknowledgement |
| AB0506 | Volume +/- or number interplay (needs full frame) |
| AB0308 | Numeric 0x08 press |
| AB0315 / AB0318 | Mode/state small frames (parser recognizes ab031e/f/03) |
| AB1119 (x4) | Repeated state transitions (ID 0x19 step/long play region) |
| AB1019 | Related to previous (short vs long variant) |
| AB0207 | Numeric 0x07 acknowledgement |
| AB0901 | Full state/frequency snapshot |
| AB0410 | Decimal point / point press ack (0x10 in position) |
| AB101C / AB0D1C / AB071C / AB081C / AB111C / AB091C / AB051C / AB0E1C | Series of quick state changes referencing 0x1C (Bluetooth / display / band-with) |
| AB020C | Freq confirm (CommandId 0x0D) truncated signature (prefix) |

NOTE: Capture truncates to first 3 bytes; real frames are 5 bytes (or more for snapshots). Interpretations thus provisional.

## 15e. Collision Resolution Strategy (Implementation)
- Maintain canonical enum for actions; alias names map to same CommandId.
- On build: if requested alias differs, log alias → canonical mapping.
- On parse: if CommandId matches known canonical, surface only canonical; maintain alias list for debugging.


## 16. Summary
The Radio-C app uses a compact custom BLE protocol: framed by header `0xAB`, fixed proto `0x02`, grouped by command class (0x0C buttons, 0x12 acknowledgements). Check bytes are linear (`base + commandId`). Two characteristics (FF13 write, FF14 notify) facilitate half-duplex control & state updates. Notifications carry hex-encoded frequency/state structures keyed by 6-char signatures (e.g., `ab0901`). Reimplementation in C# centers on constructing frames, enabling notifications, decoding signatures, and mapping UI actions to command IDs.

---
Generated reverse-engineering document – refine with live capture logs for absolute accuracy.

## 17. Capture Timing Summary
Observations from `PROTOCOL_SEQUENCE_SAMPLE.txt` (Frames 806–882 excerpt):
- Heartbeat `AB061C` appears in tight bursts (~40–60 ms apart) then larger gaps (~0.9–1.0 s), suggesting device sends periodic lightweight state while idle and faster during active UI interaction.
- Full snapshot `AB0901` occurs after clusters of input acknowledgements (e.g., after numeric/button sequence) roughly every ~3.85 s in this slice (frames 848 & 874).
- State-change storm (multiple `ABxx1C` variants) follows user interaction—likely representing layered status toggles (Bluetooth, display, bandwidth, stereo) each encoded separately.
- Handshake echo / keepalive `AB01FF` preceded by sudden activity—possible re-sync or confirmation prior to state series.
- Frequency confirm (`AB020C`) appears before a later snapshot; may indicate 'freq entry finalize' preceding full state broadcast.

Timing Heuristics for Reimplementation:
1. Treat `AB061C` as passive state tick; if absent for >2 heartbeat intervals (e.g., >2 s) consider reconnect or request manual state.
2. Listen for `AB0901` as authoritative state; on receipt, coalesce preceding short status frames to update composite model.
3. Ignore isolated `ABxx1C` bursts when a fresh snapshot is imminent (<100 ms) to prevent UI flicker; batch within a debounce window.
4. If handshake echo not observed within startup window (<1 s after sending handshake), resend handshake once.

Next Validation Step: Collect full-length frames (5+ bytes) corresponding to the truncated signatures to confirm check bytes & any payload extensions.
