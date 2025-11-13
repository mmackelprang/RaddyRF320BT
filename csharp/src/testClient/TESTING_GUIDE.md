# RF320 Protocol Testing Guide

## Current Status (November 13, 2025)

### ✅ VERIFIED
1. **GATT Characteristics**
   - TX: `0000ff13` (write commands to device)
   - RX: `0000ff14` (receive status from device)
   - Service: `0000ff12`

2. **Connection & Handshake**
   - Handshake (`AB 01 FF AB`) successfully sends
   - Device does NOT send ACK
   - Device immediately streams status messages

3. **Status Messages** (Group 0x1C)
   - Continuous stream at ~2-3 messages/second
   - Two types observed:
     - Type 0x06: 8 bytes (`AB 05 1C 06 03 01 XX YY`)
     - Type 0x08: 9 bytes (`AB 06 1C 08 03 02 XX YY ZZ`)
   - Bytes 6-7 in Type 0x08 appear to be ASCII digits (20-39)

### ⚠️ NEEDS TESTING

## Test Procedures

### Phase 1: Button Command Testing

**Goal:** Verify button commands work and understand responses

**Steps:**
1. Run app: `dotnet run`
2. Wait for connection and "Ready!" prompt
3. Test basic commands:
   - Press `1` → Should send Number1 command (`AB 02 0C 01 BA`)
   - Press `2` → Should send Number2 command (`AB 02 0C 02 BB`)
   - Press `+` → Should send VolAdd command (`AB 02 0C 12 CB`)
   - Press `-` → Should send VolDel command (`AB 02 0C 13 CC`)
   - Press `↑` → Should send UpShort command (`AB 02 0C 0E C7`)
   - Press `↓` → Should send DownShort command (`AB 02 0C 10 C9`)
   - Press `p` → Should send Power command (`AB 02 0C 14 CD`)

**Observations to make:**
- Does radio respond with any frames (ACK, status change)?
- Do status messages change after commands?
- Are there any patterns in bytes 6-7 of Type 0x08 messages?
- Does the radio physically respond (volume change, frequency change, etc.)?

**Log Analysis:**
- Check log file after testing
- Look for any Group 0x12 (ACK) frames
- Look for changes in status message patterns
- Compare "before" and "after" status messages

### Phase 2: Status Message Decoding

**Goal:** Understand what status messages mean

**Hypothesis Testing:**
1. **ASCII Digits Theory**
   - Bytes 6-7 in Type 0x08 might be frequency digits
   - Test: Press frequency-related buttons, observe changes
   - Values seen: 20, 21, 22, 23, 30, 31, 32, 33, 39

2. **State Indicators**
   - Type 0x06 vs 0x08 might represent different states
   - Test: Change radio modes, observe which type appears

3. **Checksum/Validation**
   - Last byte in each type might be checksum
   - Test: Record patterns and calculate possible checksum formulas

**Data Collection:**
Run app and let it collect status messages for 30 seconds while:
- Radio is idle
- After pressing number keys
- After pressing volume keys
- After pressing arrow keys
- After changing frequency

### Phase 3: Full State Frames

**Goal:** Capture ab0901 or ab090f frames mentioned in protocol doc

**Method:**
- These might be sent:
  - On initial connection (not seen yet)
  - After specific commands
  - Periodically
  - When frequency changes

**Test:**
1. Monitor for longer periods (2-3 minutes)
2. Try frequency entry sequence:
   - `1` `4` `6` `.` `5` `2` `Enter`
3. Try band changes: press `b`
4. Try mode changes: press `m`

### Phase 4: Command Response Timing

**Goal:** Understand if commands need specific timing or sequencing

**Tests:**
1. Rapid commands: Send multiple commands quickly
2. Slow commands: Wait 2-3 seconds between commands
3. Command pairs: Test if certain commands need to be paired
4. During status: Send command while status is streaming

## Tools & Scripts

### Quick Command Test
```powershell
# Run app and monitor specific log entries
dotnet run | Select-String "TX:|RX:|Writing|Received"
```

### Log Analysis
```powershell
# After running app, analyze the log
$log = Get-Content $env:LOCALAPPDATA\RadioClient\Logs\RadioClient_*.log | Select-Object -Last 1
$log | Select-String "Type: Button|Type: Ack|Type: Status"
```

### Status Pattern Analysis
```powershell
# Extract status messages
$log = Get-Content (Get-ChildItem $env:LOCALAPPDATA\RadioClient\Logs\*.log | Sort-Object LastWriteTime | Select-Object -Last 1).FullName
$status = $log | Select-String "AB-0[56]-1C"
$status | ForEach-Object { $_.Line -replace '.*Data: ', '' }
```

## Expected Outcomes

### If Button Commands Work:
- Device should respond with ACK frame (`AB 02 12 01 C0` for success)
- Status messages might change to reflect new state
- Physical radio should respond (volume, frequency, etc.)

### If Button Commands Don't Work:
- Might need different write method (WriteWithResponse vs WriteWithoutResponse)
- Might need specific timing
- Might need to enable different characteristic
- Might need pairing/bonding first

## Next Steps Based on Results

### Scenario A: Commands work, ACKs received
- Document command/response patterns
- Decode full state frames
- Implement frequency parsing
- Create complete command library

### Scenario B: Commands work, no ACKs
- Commands accepted but no feedback
- Rely on status stream changes for confirmation
- Update protocol doc to note no ACK behavior

### Scenario C: Commands don't work
- Try WriteWithResponse instead of WriteWithoutResponse
- Check if device needs to be paired
- Verify characteristic properties
- Check if commands need specific prefix/handshake

## Test Log Template

```
Date: 2025-11-13
Tester: [Name]
Device: RF320-BLE (D5D62AFF4241)

Command Tested: [e.g., Number1]
Expected Frame: [e.g., AB 02 0C 01 BA]
Sent Successfully: [Yes/No]
Response Received: [None/ACK/Status Change]
Physical Response: [What happened on radio]
Status Before: [Type 0x08 data]
Status After: [Type 0x08 data]
Notes: [Any observations]
```

## Files to Review After Testing
- `%LOCALAPPDATA%\RadioClient\Logs\RadioClient_*.log` - Full message log
- `PROTOCOL_INFO.md` - Update with findings
- `README.md` - Update troubleshooting section
- `RadioProtocol.cs` - Update frame parsing if needed
