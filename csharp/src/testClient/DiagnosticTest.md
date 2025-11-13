# Diagnostic Analysis: Missing ACK Messages

## Problem Statement
Commands are sent successfully to the RF320 radio, but:
1. No ACK frames (Group 0x12) are received
2. Radio UI does not reflect the changes (band/frequency/volume)
3. Only Status messages (Group 0x1C) are observed on characteristic ff14

## Hypothesis: Multiple Notification Channels

### Theory
The Android app analysis shows two characteristics in the protocol documentation:
- **ff13** (Write) - Send commands TO radio
- **ff14** (Notify) - Receive messages FROM radio

However, device discovery also revealed:
- **fff1** in service ff10 - Purpose unclear, but might be the ACK channel

**Hypothesis:** The device may use **TWO notification characteristics**:
- **ff14** - Status messages (Group 0x1C) - heartbeat/state updates
- **fff1** - ACK messages (Group 0x12) - command confirmations

### Evidence Supporting This Theory

1. **Status messages work perfectly on ff14**
   - Receiving continuous stream (~2-3 msg/sec)
   - Messages are well-formed and parseable
   - CCCD write succeeded on ff14

2. **ACK messages completely absent**
   - Zero ACK frames observed across all tests
   - Would expect at least handshake ACK
   - Android app code expects ACKs (accectSuccess/accectFaile)

3. **Alternative characteristic exists**
   - fff1 in service ff10 discovered during hardware testing
   - Marked as "purpose unclear" in documentation
   - Never attempted to enable notifications on this characteristic

4. **Device may separate notification streams**
   - Common BLE pattern: separate characteristics for different message types
   - Prevents status flood from blocking ACK delivery
   - Allows selective subscription (app only needs ACKs, embedded system might only need status)

## Proposed Solution

### Test Plan: Dual Notification Subscription

Modify `WinBleTransport.cs` to:
1. Subscribe to **fff1** for ACK messages (Group 0x12)
2. Subscribe to **ff14** for Status messages (Group 0x1C)
3. Merge notification streams into single event handler
4. Test if ACKs now appear

### Implementation Changes Required

#### WinBleTransport.cs
```csharp
// Add second RX characteristic
private GattCharacteristic? _rxStatusCharacteristic;  // ff14 for status
private GattCharacteristic? _rxAckCharacteristic;     // fff1 for ACKs

// In InitializeAsync:
// 1. Find fff1 from service ff10
// 2. Find ff14 from service ff12
// 3. Enable notifications on BOTH
// 4. Subscribe both to same handler (or separate handlers that merge)
```

### Alternative Hypotheses

If dual subscription doesn't work, other possibilities:

#### Hypothesis 2: Handshake Response Required
**Theory:** Device expects a specific response to handshake before accepting commands
- Current: Send AB 01 FF AB, wait for any message, proceed
- Should be: Send AB 01 FF AB, wait for specific ACK, send confirmation, then proceed

#### Hypothesis 3: Wrong TX Characteristic
**Theory:** Commands should be sent to a different characteristic
- Current: Writing to ff13
- Alternative: Maybe ff15 (seen in service ff12 during discovery)?
- Test: Try writing to all writable characteristics

#### Hypothesis 4: Command Format Issue
**Theory:** Frame format differs from documentation
- Checksum algorithm different?
- Additional bytes required?
- Byte order wrong?
- Test: Capture Android app BLE traffic with sniffer

#### Hypothesis 5: Pairing/Bonding Required
**Theory:** Device requires secure connection before accepting commands
- Current: Connecting without pairing
- Should be: Pair/bond device first
- Evidence: Some BLE devices reject commands from unpaired clients

#### Hypothesis 6: Radio in Wrong Mode
**Theory:** Radio must be in specific mode to accept BLE commands
- Maybe needs to be powered on in "Bluetooth control mode"
- Physical button sequence to enable BLE control?
- Radio might be in "read-only status broadcast" mode

## Testing Priority

1. **HIGH PRIORITY: Test dual notification subscription (fff1 + ff14)**
   - Quick code change
   - Directly addresses missing ACK issue
   - Aligns with discovered hardware characteristics

2. **MEDIUM PRIORITY: Analyze Android app BLE logs**
   - Use Android BLE sniffer (btsnoop_hci.log)
   - See actual bytes exchanged
   - Confirm characteristic usage

3. **MEDIUM PRIORITY: Test pairing/bonding**
   - Try Windows "Pair device" before running app
   - See if pairing enables command acceptance

4. **LOW PRIORITY: Test alternative TX characteristics**
   - Try ff15 if it exists and is writable
   - Test all write-capable characteristics

5. **LOW PRIORITY: Consult radio manual**
   - Check if BLE control mode needs activation
   - Verify button sequence for BLE pairing

## Expected Outcome

If dual subscription is the solution:
- ACK frames should appear on fff1 characteristic
- Radio UI should reflect commands (band/volume/frequency changes)
- Handshake ACK should appear: AB 02 12 01 C0

If not, we systematically work through alternative hypotheses.
