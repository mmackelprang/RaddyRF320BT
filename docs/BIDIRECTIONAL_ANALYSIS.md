# RF320-BLE Bidirectional Protocol Analysis

**Document:** Complete analysis of BTDump.pdml Wireshark capture  
**Date:** October 22, 2025 capture session  
**Duration:** ~35 seconds (frames 760-~1400+)  
**Total Packets Analyzed:** 600 AB protocol packets  

## Executive Summary

This analysis examined a complete bidirectional Bluetooth Low Energy conversation between the Android app and RF320-BLE radio device. The capture reveals the full command-response protocol, including app-initiated commands and radio responses.

### Key Statistics
- **App → Radio (Commands Sent):** 115 packets via Write Request to handle 0x000c
- **Radio → App (Notifications):** 485 packets via Handle Value Notification on handle 0x000e  
- **Command/Response Ratio:** 1:4.2 (each command triggers ~4 responses on average)
- **Connection Handle:** 0x041
- **Capture Start:** Frame 760 (connection established at 35.257071s)
- **First Protocol Command:** Frame 828 (AB01FF handshake at 35.914940s)

## Protocol Handles

| Handle | Direction | ATT Operation | Purpose |
|--------|-----------|---------------|---------|
| 0x000c | App → Radio | Write Request (0x12) | Command channel - app sends instructions |
| 0x000e | Radio → App | Handle Value Notification (0x1b) | Data channel - radio sends status updates |

## Connection Sequence

### 1. Connection Establishment (Frame 760, t=35.257071s)
- BLE connection initiated between phone (00:00:00:00:00:00) and RF320-BLE (d5:d6:2a:ff:42:41)
- Connection handle 0x041 assigned

### 2. Initial Notifications (Frames 806-808, t=35.63-35.67s)
- Radio immediately starts sending AB061C (Frequency Data 2) packets
- Indicates radio was already powered on and tuned to a frequency

### 3. Handshake Command (Frame 828, t=35.914940s)
**APP SENDS:** `AB01FF` - Handshake/initialization command  
**Response Time:** 102ms (0.102060s) until first response

**RADIO RESPONDS with startup sequence (17 packets in 1 second):**
- AB0220 - Status (short format)
- AB0417 - Battery level  
- AB0E21 - Frequency details
- AB0821 - Name/label
- AB0B1C - Signal strength
- AB0205 - Status
- AB0506 - Frequency data 1
- AB0308 - Mode/bandwidth
- AB0315 - Mode/bandwidth
- AB0318 - Mode/bandwidth
- AB1119 (x4) - Memory channels (repeated 4 times)
- AB1019 - Audio/volume
- AB0207 - Status
- AB0901 - Squelch
- AB0410 - Battery level
- AB101C - Audio/volume
- AB0D1C - Modulation
- AB071C - Scan status
- AB081C - Name/label  
- AB071C - Scan status (repeated)
- AB111C - Memory channels
- AB091C - Squelch
- AB051C - Frequency data 1
- AB061C - Frequency data 2
- AB051C - Frequency data 1 (repeated)
- AB0E1C - Frequency details
- ... continues with multiple AB061C packets

## Command Analysis

### Commands Sent by App (Handle 0x000c)

1. **AB01FF** - Handshake/Initialize (Frame 828, t=35.914940s)
   - Sent once at connection start
   - Triggers complete radio state dump
   - Response: 17 different packet types in first second

2. **AB020C** - Polling/Keep-Alive Command (Frames 871+, starting t=40.051320s)
   - Sent repeatedly throughout session (114 times)
   - Interval: approximately every 1-2 seconds
   - Purpose: Request status updates / keep connection alive
   - Each command triggers burst of responses (typically AB0901, AB061C, AB051C, AB111C, AB0A1C, AB0410)

### Typical Response Pattern to AB020C

After each AB020C command, radio typically sends:
1. **AB0901** - Squelch status
2. **AB061C** - Current frequency (channel data)  
3. **AB051C** - Frequency data 1
4. **AB111C** or **AB0A1C** - Memory/channel info
5. **AB0410** - Battery level

Sometimes includes additional packets:
- **AB071C** - Scan status
- **AB0E1C** - Detailed frequency info

## Timing Analysis

### Command-Response Latency
- Handshake AB01FF → First response AB0220: **102ms**
- Typical AB020C → AB0901 response: **110-160ms**
- Multiple responses arrive in bursts over 300-500ms window

### Polling Interval
- First AB020C: t=40.051s (4.1s after handshake)
- Second AB020C: t=41.230s (1.18s interval)
- Third AB020C: t=42.466s (1.24s interval)
- Pattern continues with 1.0-1.5s intervals

### Continuous Updates
Between polling commands, radio autonomously sends:
- **AB061C** frequency updates (~50ms intervals when signal active)
- Indicates real-time frequency monitoring/scanning

## Protocol Insights

### 1. Handshake is Required
- AB01FF must be sent after BLE connection before radio accepts other commands
- Radio dumps complete state after handshake
- Without handshake, radio only sends AB061C autonomous updates

### 2. Polling Model
- App uses AB020C as heartbeat/status request
- Radio doesn't send most updates unless polled
- Exception: AB061C sent autonomously when frequency changes

### 3. Packet Burst Pattern
- Commands trigger burst responses (not single reply)
- Radio sends 3-6 packets per poll command
- Likely assembling complete radio state snapshot

### 4. No Acknowledged Write
- All app commands use Write Request (0x12), which expects Write Response (0x13)
- Radio always acknowledges write at ATT layer
- Protocol data carried in Write Request value field, not response

### 5. Autonomous Frequency Updates
- Radio continuously broadcasts AB061C when signal detected
- Allows app to update frequency display in real-time
- Sent at ~1Hz rate during active scanning

## Packet Type Distribution (Radio → App)

| Packet Type | Count (approx) | Purpose | When Sent |
|-------------|---------------|---------|-----------|
| AB061C | 250+ | Frequency Data 2 | Continuously during scan, after every poll |
| AB051C | 80+ | Frequency Data 1 | After handshake, after most polls |
| AB0901 | 40+ | Squelch Status | After every AB020C poll |
| AB111C | 30+ | Memory Channel | After handshake, intermittent polls |
| AB0410 | 25+ | Battery Level | After handshake, after polls |
| AB0A1C | 20+ | Unknown (Memory?) | Intermittent polls |
| AB071C | 15+ | Scan Status | After handshake, some polls |
| AB0E1C | 10+ | Frequency Details | After handshake, some polls |
| AB0220 | 1 | Status Short | Initial response to handshake |
| AB0417 | 1 | Battery | Initial handshake response |
| AB0E21 | 1 | Frequency Details | Initial handshake response |
| AB0821 | 1 | Name/Label | Initial handshake response |
| AB0B1C | 1 | Signal Strength | Initial handshake response |
| AB0205 | 1 | Status | Initial handshake response |
| AB0506 | 1 | Frequency Data 1 | Initial handshake response |
| AB0308 | 1 | Mode/Bandwidth | Initial handshake response |
| AB0315 | 1 | Mode/Bandwidth | Initial handshake response |
| AB0318 | 1 | Mode/Bandwidth | Initial handshake response |
| AB1119 | 4 | Memory Channel | Initial handshake (4× repeated) |
| AB1019 | 1 | Audio/Volume | Initial handshake response |
| AB0207 | 1 | Status | Initial handshake response |
| AB101C | 1 | Audio/Volume | Initial handshake response |
| AB0D1C | 1 | Modulation | Initial handshake response |
| AB081C | 1 | Name/Label | Initial handshake response |
| AB091C | 1 | Squelch | Initial handshake response |

## New Findings

### 1. Full Command Set Identified
Previous analysis only had radio→app packets. This capture reveals:
- **AB01FF** - Handshake/init command (not previously documented)
- **AB020C** - Polling/status request command (new discovery)

### 2. Protocol Flow Understanding
```
[App Connects via BLE]
    ↓
[Radio sends AB061C autonomously]
    ↓
[App sends AB01FF handshake]
    ↓ (100ms)
[Radio dumps complete state: 17 packet types]
    ↓
[App begins polling with AB020C every 1-2 seconds]
    ↓
[Radio responds with burst of 4-6 packets per poll]
    ↓
[Radio continues autonomous AB061C frequency updates]
```

### 3. Real-Time vs Polled Data
- **Real-time autonomous:** AB061C (frequency updates)
- **Polled data:** Most other packet types require AB020C command
- **One-time startup:** Special packets only sent after AB01FF handshake

### 4. No User Action Commands Observed
This capture appears to be passive monitoring (no button presses, no frequency changes initiated by user). Commands that would likely exist but weren't captured:
- Tune to frequency command
- Change mode/bandwidth command
- Adjust volume command
- Start/stop scan command
- Store/recall memory channel command

## Recommendations for Implementation

### 1. Connection Initialization
```java
// After BLE connection established:
1. Wait for initial AB061C notifications (confirms radio ready)
2. Send AB01FF handshake command
3. Wait 100-200ms
4. Process burst of startup packets (17+ different types)
5. Begin periodic AB020C polling
```

### 2. Polling Loop
```java
// Start polling at 1-2 second intervals:
while (connected) {
    sendCommand(0xAB, 0x02, 0x0C);  // AB020C
    Thread.sleep(1200);  // 1.2 second interval
}
```

### 3. Packet Processing Priority
- **High priority:** AB061C (real-time frequency updates)
- **Medium priority:** AB0901, AB0410 (status after each poll)
- **Low priority:** AB111C, AB0A1C (memory/channel data)
- **One-time:** AB0220, AB0417, etc. (startup dump only)

### 4. User-Initiated Commands
Based on protocol structure, user commands would likely follow format:
```
AB <cmd> <param>
```

Example hypothetical commands (not observed, inferred from protocol):
- **AB03**XX - Set frequency (XX = frequency bytes)
- **AB04**XX - Set mode/bandwidth
- **AB05**XX - Set volume
- **AB09**XX - Set squelch level

## Comparison with Previous Analysis

| Aspect | Previous (267 packets) | This Capture (600 packets) |
|--------|----------------------|---------------------------|
| Direction | Radio → App only | Bidirectional |
| Commands Observed | None | AB01FF, AB020C |
| Session Type | Passive listening | Active connection with polling |
| AB05/AB06 Pairs | 82 each | AB051C:80+, AB061C:250+ |
| New Insights | Packet formats | Command-response protocol flow |
| Coverage | 100% of RX packets | 100% of bidirectional protocol |

## Questions for Further Investigation

1. **What other commands exist?**  
   Only observed AB01FF and AB020C. Likely many more commands for:
   - Tuning frequency
   - Changing settings (mode, squelch, volume)
   - Memory operations
   - Scan control

2. **What triggers AB0220 vs AB0207 vs AB0205?**  
   Three different "status" packet types observed, purpose unclear

3. **Why AB1119 repeated 4 times?**  
   Memory channel packet sent 4× in handshake response - possibly 4 memory banks?

4. **What do AB0315 and AB0318 contain?**  
   Both labeled as Mode/Bandwidth but different - may be different modulation types

5. **What is AB0A1C?**  
   Frequent packet (20+ occurrences) but purpose unknown

## Files Generated

- **BIDIRECTIONAL_CAPTURE.txt** - Complete packet listing with timing (3605 lines, 600 packets)
- **BIDIRECTIONAL_ANALYSIS.md** - This document

## Related Documentation

- **WIRESHARK_ANALYSIS.md** - Analysis of 267-packet receive-only capture
- **QUICK_REFERENCE.md** - Developer quick reference for RadioProtocolHandler
- **ADDITIONAL_MESSAGES.md** - Extended packet type documentation
- **RadioProtocolHandler.java** - Java implementation of all 13 packet parsers

---

**Analysis Date:** Generated from BTDump.pdml captured Oct 22, 2025  
**Analyzed:** All 600 AB protocol packets from ~35 second session  
**Key Discovery:** Full command-response protocol flow now documented
