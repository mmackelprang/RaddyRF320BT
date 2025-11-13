# Radio-C Status Message Deep Dive Analysis

> **Analysis Date:** November 13, 2025  
> **Focus:** Decoding Frequency, Band, and Signal-to-Noise Ratio (SNR) from status messages  
> **Source Files:** MainActivity.smali and related inner classes

## Executive Summary

The Radio-C device sends status updates via BLE notifications on characteristic `0000ff14-0000-1000-8000-00805f9b34fb`. The primary status message signature is **`ab0901`** (full state snapshot) with an alternate **`ab090f`** for input mode. These messages encode:

1. **Frequency** - 8 hex digits assembled from 4 byte pairs, converted to decimal
2. **Band** - Single byte indicator (00=FM, 01=AM, 02=SW, 03=AIR, etc.)
3. **Signal Quality/SNR** - High nibble of a byte used to display signal strength bars (0-6 levels)

---

## 1. Frequency Decoding (`ab0901` and `ab090f`)

### 1.1 Message Structure: `ab0901` (Primary Status)

The `ab0901` signature represents a full state snapshot. After the 6-char signature (`ab0901`), the payload is parsed as follows:

```
Hex String Layout (zero-indexed positions):
0-5:   Signature "ab0901"
6-7:   at  - Mode/state byte (00=FM, 01=AM, 02=SW, 03=AIR, etc.)
8-9:   ax  - State field 1
10-11: ay  - State field 2
12-13: az  - State field 3
14-15: aA  - State field 4
16-17: Frequency byte 4 (v6)
18-19: Frequency byte 3 (v5)
20-21: Frequency byte 2 (v11)
22-23: Frequency byte 1 (v1)
24-25: cj  - Scanner/animation control (00=show, 01=hide, 02=flicker)
26-27: Unit byte (00=MHz, 01=KHz)
```

### 1.2 Frequency Assembly Algorithm

**From MainActivity.smali lines 3506-3520:**

```java
// Pseudo-code reconstruction from smali:

// Extract 4 frequency bytes (positions 16-24, reading backwards)
String v6 = hexString.substring(16, 18);  // Byte 4
String v5 = hexString.substring(18, 20);  // Byte 3
String v11 = hexString.substring(20, 22); // Byte 2
String v1 = hexString.substring(22, 24);  // Byte 1

// Concatenate in reverse order: v6 + v5 + v11 + v1
String frequencyHex = v6 + v5 + v11 + v1;  // e.g., "0009702d"
this.ch = frequencyHex;

// Convert hex to decimal string
String frequencyDecimal = hexToDec(frequencyHex);
this.K = frequencyDecimal;
```

### 1.3 Nibble Extraction for UI Display

**From MainActivity.smali lines 3551-3558:**

The frequency parsing also extracts nibbles from a specific byte at position 18-20 (segment at offset 0x12-0x14):

```java
// Get byte from position 18-20 
String segmentByte = hexString.substring(18, 20);  
byte[] segmentBytes = hexStringToBytes(segmentByte);

// Extract low nibble (bits 0-3)
int af = segmentBytes[0] & 0x0F;

// Extract high nibble (bits 4-7)  
int ag = (segmentBytes[0] >> 4) & 0x0F;
```

**Purpose:**
- `af` (low nibble) - Used for UI icon/mode display
- `ag` (high nibble) - **Controls signal strength bar display (0-6 levels)**

### 1.4 Unit Detection

**From MainActivity.smali lines 3640-3660:**

```java
String unitByte = hexString.substring(16, 18);  // Position 16-18 (byte at 0x10)

if (unitByte.equals("00")) {
    // Display "MHz"
    runOnUiThread(new MainActivity$170(this));
}

if (unitByte.equals("01")) {
    // Display "KHz"  
    runOnUiThread(new MainActivity$171(this));
}
```

### 1.5 Alternate Format: `ab090f` (Input Mode)

**From MainActivity.smali lines 3744-3860:**

When frequency input mode is active, the device sends `ab090f` with a slightly different layout:

```
Hex String Layout:
0-5:   Signature "ab090f"
6-7:   Input mode indicator
8-9:   at - Mode byte
10-11: ax - State field 1
12-13: ay - State field 2
14-15: az - State field 3
16-17: aA - State field 4
18-19: Frequency byte 4
20-21: Frequency byte 3
22-23: Frequency byte 2
24-25: Frequency byte 1
26-27: Nibble control byte (af/ag extracted from this)
28-29: Unit byte (00=MHz, 01=KHz)
```

Assembly is similar but at different offsets:

```java
String v6 = hexString.substring(16, 18);
String v1 = hexString.substring(18, 20);
String v11 = hexString.substring(20, 22);
String v4 = hexString.substring(22, 24);

String frequencyHex = v6 + v1 + v11 + v4;
String frequencyDecimal = hexToDec(frequencyHex);
this.K = frequencyDecimal;
```

---

## 2. Band Decoding

### 2.1 Band Field Location

**From MainActivity.smali lines 3427-3437:**

The band is encoded in the `at` field, extracted immediately after the signature:

```java
// For ab0901 signature:
String at = hexString.substring(6, 8);  // Position 6-8
this.at = at;
```

### 2.2 Band Value Mapping

**From MainActivity$165.smali (Runnable triggered after parsing):**

The `at` field is compared to determine the band and update the UI:

```java
// Line 2090-2098 of MainActivity logic:

if (this.at.equals("00")) {
    // Band TextView displays "FM"
}

if (this.at.equals("01")) {
    // Band TextView displays "AM"
}

if (this.at.equals("02")) {
    // Band TextView displays "SW" (Short Wave)
}

if (this.at.equals("03")) {
    // Band TextView displays "AIR" (Aircraft/Airband)
}
```

**Additional modes beyond 03 likely exist but were not observed in the decompiled code.**

### 2.3 Band Encoding Summary

| Hex Value (`at`) | Band Name | Description |
|------------------|-----------|-------------|
| `00` | FM | FM Radio (87.5-108 MHz typical) |
| `01` | AM | AM Radio (530-1710 KHz typical) |
| `02` | SW | Short Wave |
| `03` | AIR | Airband (Aviation frequencies) |
| `04`-`FF` | Reserved | Additional bands (VHF, UHF, etc.) |

---

## 3. Signal-to-Noise Ratio (SNR) / Signal Strength Decoding

### 3.1 SNR Representation

The signal strength is **NOT directly encoded as a traditional SNR value (dB)**, but rather as a **visual signal bar level** from 0 to 6. This is derived from the **high nibble (`ag`)** of a specific byte in the status message.

### 3.2 Signal Bar Control Logic

**From MainActivity.smali lines 919-1270 (method `a(MainActivity, int af, int ag, String K)`):**

The method receives three parameters:
- `af` (int) - Low nibble, used for other UI indicators
- `ag` (int) - **High nibble, controls signal strength bars (0-6)**
- `K` (String) - Frequency string

**Signal Bar Display Logic:**

```java
// p1 = af (low nibble)
// p2 = ag (high nibble) - THIS IS THE SIGNAL STRENGTH LEVEL
// p3 = K (frequency)

// There are 6 TextViews representing signal bars: X, Y, Z, aa, ab, ac
// Visibility is controlled by the ag value (0-6):

if (p1 == 0) {
    // Hide all bars
    X.setVisibility(INVISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 1) {
    // Show 1 bar
    X.setVisibility(VISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 2) {
    // Show 2 bars
    X.setVisibility(INVISIBLE);
    Y.setVisibility(VISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 3) {
    // Show 3 bars
    X.setVisibility(INVISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(VISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 4) {
    // Show 4 bars
    X.setVisibility(INVISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(VISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 5) {
    // Show 5 bars
    X.setVisibility(INVISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(VISIBLE);
    ac.setVisibility(INVISIBLE);
}

if (p1 == 6) {
    // Show 6 bars (full strength)
    X.setVisibility(INVISIBLE);
    Y.setVisibility(INVISIBLE);
    Z.setVisibility(INVISIBLE);
    aa.setVisibility(INVISIBLE);
    ab.setVisibility(INVISIBLE);
    ac.setVisibility(VISIBLE);
}
```

**NOTE:** The logic shows only ONE bar TextView visible at a time (not cumulative). This suggests either:
1. The bars are positioned to show increasing size/position, OR
2. There's a visual layout where each TextView represents a different signal level graphic

### 3.3 SNR Byte Location

**The byte containing `ag` (high nibble for signal strength) is extracted from position 18-20 in `ab0901` messages:**

```java
// Position 18-20 (hex chars 18-19)
String nibbleByte = hexString.substring(18, 20);
byte[] bytes = hexStringToBytes(nibbleByte);

// Extract high nibble (signal strength level 0-6)
int ag = (bytes[0] >> 4) & 0x0F;
```

### 3.4 Signal Strength Summary

| `ag` Value | Signal Bars | Signal Quality |
|------------|-------------|----------------|
| 0 | None | No signal / Searching |
| 1 | 1 bar | Very weak |
| 2 | 2 bars | Weak |
| 3 | 3 bars | Fair |
| 4 | 4 bars | Good |
| 5 | 5 bars | Very good |
| 6 | 6 bars | Excellent |
| 7-15 | Reserved | Not used in observed code |

---

## 4. Complete Message Parsing Example

### 4.1 Sample `ab0901` Message

```
Hex String: ab09010009702d0000001234567800
```

**Breakdown:**

| Position | Bytes | Field | Value | Decoded |
|----------|-------|-------|-------|---------|
| 0-5 | ab0901 | Signature | ab0901 | Status snapshot |
| 6-7 | 00 | at (Band) | 00 | FM |
| 8-9 | 09 | ax | 09 | State field 1 |
| 10-11 | 70 | ay | 70 | State field 2 |
| 12-13 | 2d | az | 2d | State field 3 |
| 14-15 | 00 | aA | 00 | State field 4 |
| 16-17 | 00 | Freq byte 4 | 00 | - |
| 18-19 | 00 | Freq byte 3 / Nibble | 00 | ag=0, af=0 (no signal) |
| 20-21 | 12 | Freq byte 2 | 12 | - |
| 22-23 | 34 | Freq byte 1 | 34 | - |
| 24-25 | 56 | cj (Scanner) | 56 | Animation control |
| 26-27 | 78 | Unit | 78 | (not 00/01, likely error) |

**Frequency Assembly:**
```
Hex: 00 + 00 + 12 + 34 = "00001234"
Decimal: hexToDec("00001234") = "4660"
Displayed: 4.660 (with format "0.000")
```

**Band:** `00` = FM

**Signal Strength:** High nibble of byte at position 18-19 = `0x00 >> 4 = 0` (no signal bars)

### 4.2 Realistic Example

```
Hex String: ab090100003f845200000050
```

| Position | Bytes | Field | Decoded |
|----------|-------|-------|---------|
| 0-5 | ab0901 | Signature | Status |
| 6-7 | 00 | at | FM |
| 8-9 | 00 | ax | - |
| 10-11 | 3f | ay | - |
| 12-13 | 84 | az | - |
| 14-15 | 52 | aA | - |
| 16-17 | 00 | Freq 4 | - |
| 18-19 | 00 | Freq 3 / Nibble | ag=0, af=0 |
| 20-21 | 00 | Freq 2 | - |
| 22-23 | 50 | Freq 1 | - |

**Frequency:** `"00000050"` → decimal 80 → displayed as frequency (scaling TBD via hexToDec)

---

## 5. Additional State Fields

### 5.1 Scanner/Animation Control (`cj`)

**From MainActivity.smali lines 930-976:**

The `cj` field at position 20-22 (0x14-0x16) controls a scanner animation:

```java
if (cj.equals("00")) {
    // Show scanner animation (LinearLayout visible, not flickering)
    scannerLayout.clearAnimation();
    scannerLayout.setVisibility(VISIBLE);
}

if (cj.equals("01")) {
    // Hide scanner animation
    scannerLayout.clearAnimation();
    scannerLayout.setVisibility(INVISIBLE);
}

if (cj.equals("02")) {
    // Flicker scanner animation
    scannerLayout.setVisibility(VISIBLE);
    flicker(scannerLayout);
}
```

### 5.2 Special Frequency Case

**From MainActivity.smali lines 3586-3600:**

If `at == "06"` AND `ch == "000000ff"`, the device is in a special scanning state:

```java
if (this.at.equals("06") && this.ch.equals("000000ff")) {
    // Trigger scanning UI state (MainActivity$168 runnable)
    runOnUiThread(new MainActivity$168(this));
} else {
    // Normal frequency display
    sendMessage(0x3F2);  // Trigger UI update
}
```

---

## 6. Implementation Guidance for C#

### 6.1 Data Structure

```csharp
public class RadioStatusMessage
{
    public string Signature { get; set; }          // "ab0901" or "ab090f"
    public string Band { get; set; }               // "00"=FM, "01"=AM, etc.
    public string FrequencyHex { get; set; }       // 8 hex chars
    public string FrequencyDecimal { get; set; }   // Converted decimal
    public double FrequencyMHz { get; set; }       // Scaled for display
    public int SignalStrength { get; set; }        // 0-6 (from ag nibble)
    public int LowNibble { get; set; }             // af
    public int HighNibble { get; set; }            // ag
    public bool UnitIsMHz { get; set; }            // true=MHz, false=KHz
    public string ScannerState { get; set; }       // cj field
    
    // State fields
    public string StateAx { get; set; }
    public string StateAy { get; set; }
    public string StateAz { get; set; }
    public string StateAA { get; set; }
}
```

### 6.2 Parser Method

```csharp
public static RadioStatusMessage Parse(byte[] notificationValue)
{
    string hex = BitConverter.ToString(notificationValue)
        .Replace("-", "")
        .ToLowerInvariant();
    
    if (hex.Length < 28) return null;
    
    string signature = hex.Substring(0, 6);
    
    if (signature == "ab0901")
    {
        return new RadioStatusMessage
        {
            Signature = signature,
            Band = hex.Substring(6, 2),
            StateAx = hex.Substring(8, 2),
            StateAy = hex.Substring(10, 2),
            StateAz = hex.Substring(12, 2),
            StateAA = hex.Substring(14, 2),
            
            // Frequency bytes (positions 16-24)
            FrequencyHex = hex.Substring(16, 2) + hex.Substring(18, 2) + 
                          hex.Substring(20, 2) + hex.Substring(22, 2),
            
            // Nibbles from position 18-20
            LowNibble = Convert.ToByte(hex.Substring(18, 2), 16) & 0x0F,
            HighNibble = (Convert.ToByte(hex.Substring(18, 2), 16) >> 4) & 0x0F,
            SignalStrength = (Convert.ToByte(hex.Substring(18, 2), 16) >> 4) & 0x0F,
            
            // Scanner and unit
            ScannerState = hex.Substring(20, 2),
            UnitIsMHz = hex.Substring(26, 2) == "00",
            
            // Convert frequency
            FrequencyDecimal = HexToDecString(
                hex.Substring(16, 2) + hex.Substring(18, 2) + 
                hex.Substring(20, 2) + hex.Substring(22, 2)),
            
            FrequencyMHz = CalculateFrequency(
                hex.Substring(16, 2) + hex.Substring(18, 2) + 
                hex.Substring(20, 2) + hex.Substring(22, 2),
                hex.Substring(26, 2) == "00")
        };
    }
    else if (signature == "ab090f")
    {
        // Similar parsing with adjusted offsets
        // ... (see section 1.5 for offsets)
    }
    
    return null;
}

public static string GetBandName(string bandCode)
{
    return bandCode switch
    {
        "00" => "FM",
        "01" => "AM",
        "02" => "SW",
        "03" => "AIR",
        _ => "UNKNOWN"
    };
}

public static string HexToDecString(string hexFreq)
{
    // Implement hex-to-decimal conversion
    // The app uses a specific method; may need empirical testing
    uint value = Convert.ToUInt32(hexFreq, 16);
    return value.ToString();
}

public static double CalculateFrequency(string hexFreq, bool isMHz)
{
    uint raw = Convert.ToUInt32(hexFreq, 16);
    
    // Scaling factor needs empirical validation
    // Likely /1000 or /10000 depending on band
    if (isMHz)
        return raw / 1000.0;  // Example: 145125 → 145.125 MHz
    else
        return raw / 10.0;    // Example: 10800 → 1080.0 KHz
}
```

### 6.3 Usage Example

```csharp
// In BLE notification handler
private void OnCharacteristicValueChanged(GattCharacteristic sender, 
    GattValueChangedEventArgs args)
{
    byte[] value = new byte[args.CharacteristicValue.Length];
    DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(value);
    
    var status = RadioStatusMessage.Parse(value);
    if (status != null)
    {
        Console.WriteLine($"Band: {GetBandName(status.Band)}");
        Console.WriteLine($"Frequency: {status.FrequencyMHz:F3} " + 
            (status.UnitIsMHz ? "MHz" : "KHz"));
        Console.WriteLine($"Signal: {status.SignalStrength}/6 bars");
        
        UpdateUI(status);
    }
}
```

---

## 7. Testing & Validation

### 7.1 Empirical Validation Steps

1. **Capture Real Messages**: Use a BLE sniffer or logging to capture full `ab0901` messages
2. **Verify Frequency Scaling**: Compare parsed frequency with displayed value on device
3. **Test All Bands**: Switch through FM/AM/SW/AIR modes and verify `at` field changes
4. **Signal Strength Correlation**: Move device to different locations and observe `ag` nibble values
5. **Unit Byte Verification**: Confirm positions of MHz/KHz indicator byte

### 7.2 Known Gaps

- **Exact frequency scaling formula**: The `hexToDec` method implementation is not visible in smali; needs reverse engineering or testing
- **State fields `ax`, `ay`, `az`, `aA`**: Semantic meaning unknown; likely modulation, step size, stereo mode, squelch
- **Bands beyond 03**: Additional band codes may exist (VHF, UHF, Marine, Weather)
- **Alternative signatures**: Signatures like `ab0417`, `ab031e`, `ab031f` handle partial updates but details are incomplete

---

## 8. Quick Reference

### 8.1 Message Signatures

| Signature | Purpose | Key Fields |
|-----------|---------|------------|
| `ab0901` | Full status snapshot | Band, Frequency, Signal, Unit |
| `ab090f` | Input mode status | Same as ab0901, different offsets |
| `ab0417` | Button state update | UI state indicators (3 bytes) |
| `ab031e` | Partial state update | 2 bytes of state data |
| `ab031f` | Partial state update | 2 bytes of state data |

### 8.2 Critical Byte Positions (`ab0901`)

| Byte Position | Field | Purpose |
|---------------|-------|---------|
| 6-7 | `at` | Band (00=FM, 01=AM, 02=SW, 03=AIR) |
| 8-9 | `ax` | State field 1 |
| 10-11 | `ay` | State field 2 |
| 12-13 | `az` | State field 3 |
| 14-15 | `aA` | State field 4 |
| 16-23 | Frequency | 4 bytes assembled as 8 hex chars |
| 18-19 | Nibble byte | Low=`af`, High=`ag` (signal strength) |
| 20-21 | `cj` | Scanner animation control |
| 26-27 | Unit | 00=MHz, 01=KHz |

### 8.3 Band Codes

```
00 = FM
01 = AM
02 = SW (Short Wave)
03 = AIR (Airband)
04-FF = Reserved/Unknown
```

### 8.4 Signal Strength Levels

```
0 = No signal
1 = Very weak (1 bar)
2 = Weak (2 bars)
3 = Fair (3 bars)
4 = Good (4 bars)
5 = Very good (5 bars)
6 = Excellent (6 bars)
```

---

## 9. Conclusion

The Radio-C status messages use a compact binary protocol where:

1. **Frequency** is encoded as 4 hex byte pairs (8 chars total) concatenated and converted via `hexToDec()`
2. **Band** is a single byte immediately after the signature (00/01/02/03 for FM/AM/SW/AIR)
3. **Signal strength** is derived from the high nibble of a specific byte (position 18-19), providing 7 levels (0-6)

The primary challenge for reimplementation is determining the exact frequency scaling algorithm used by `hexToDec()`, which requires empirical testing with real device captures. All other fields are straightforward byte extractions and comparisons.

**Recommended Next Steps:**
1. Implement message parser in C# using provided structure
2. Capture real BLE messages during device operation
3. Validate frequency scaling through comparison with device display
4. Map remaining state fields (`ax`, `ay`, `az`, `aA`) through systematic testing

---

**Document Version:** 1.0  
**Last Updated:** November 13, 2025  
**Author:** AI Analysis of MainActivity.smali reverse engineering
