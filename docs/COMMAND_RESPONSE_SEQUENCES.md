# Protocol Command-Response Sequences

## Sequence 1: Initial Handshake (Frame 828, t=35.914940s)

### APP SENDS: AB01FF (Handshake)
**Frame 828** | **Time: 35.914940s** | **Handle: 0x000c** | **Direction: SENT**  
```
Value: AB01FF
```

### RADIO RESPONDS: Complete State Dump (102ms latency)
**Response begins at t=36.016660s (+0.102060s)**

| Frame | Time (s) | Δt (ms) | Packet | Description |
|-------|----------|---------|--------|-------------|
| 832 | 36.0167 | +0.1 | AB0220 | Status (short format) |
| 833 | 36.0171 | +0.4 | AB0417 | Battery level (detailed) |
| 834 | 36.0667 | +49.6 | AB0E21 | Frequency details |
| 835 | 36.0674 | +0.7 | AB0821 | Name/Label |
| 836 | 36.1150 | +47.6 | AB0B1C | Signal strength |
| 837 | 36.1161 | +1.0 | AB0205 | Status |
| 838 | 36.1637 | +47.6 | AB0506 | Frequency data 1 (detailed) |
| 839 | 36.2128 | +49.1 | AB0308 | Mode/Bandwidth |
| 840 | 36.2674 | +54.5 | AB0315 | Mode/Bandwidth variant |
| 841 | 36.2687 | +1.3 | AB0318 | Mode/Bandwidth variant |
| 842 | 36.3101 | +41.5 | AB1119 | Memory channel |
| 843 | 36.3111 | +1.0 | AB1119 | Memory channel (repeat) |
| 844 | 36.3590 | +47.9 | AB1119 | Memory channel (repeat) |
| 845 | 36.4075 | +48.5 | AB1119 | Memory channel (repeat) |
| 846 | 36.4558 | +48.3 | AB1019 | Audio/Volume |
| 847 | 36.5043 | +48.5 | AB0207 | Status variant |
| 848 | 36.5046 | +0.3 | AB0901 | Squelch status |
| 849 | 36.5541 | +49.5 | AB0410 | Battery level (short) |
| 850 | 36.5568 | +2.7 | AB101C | Audio/Volume variant |
| 851 | 36.6022 | +45.4 | AB0D1C | Modulation |
| 852 | 36.6517 | +49.5 | AB071C | Scan status |
| 853 | 36.7000 | +48.3 | AB081C | Name/Label variant |
| 854 | 36.7009 | +0.9 | AB071C | Scan status (repeat) |
| 855 | 36.7485 | +47.6 | AB111C | Memory channels variant |
| 856 | 36.7492 | +0.7 | AB091C | Squelch variant |
| 857 | 36.7972 | +48.0 | AB051C | Frequency data 1 |
| 858 | 36.8464 | +49.2 | AB061C | Frequency data 2 |
| 859 | 36.8481 | +1.7 | AB051C | Frequency data 1 (repeat) |
| 860 | 36.8949 | +46.8 | AB0E1C | Frequency details variant |
| 861 | 36.9442 | +49.3 | AB061C | Frequency data 2 (repeat) |

**Total: 30 packets in 1.03 seconds**

---

## Sequence 2: First Polling Command (Frame 871, t=40.051320s)

### APP SENDS: AB020C (Status Request/Poll)
**Frame 871** | **Time: 40.051320s** | **Handle: 0x000c** | **Direction: SENT**  
```
Value: AB020C
```

### RADIO RESPONDS: Status Burst (110ms latency)
**Response begins at t=40.161477s (+0.110157s)**

| Frame | Time (s) | Δt (ms) | Packet | Description |
|-------|----------|---------|--------|-------------|
| 874 | 40.1615 | +0.0 | AB0901 | Squelch status |
| 875 | 40.3586 | +197.1 | AB061C | Frequency data 2 |
| 876 | 40.4047 | +46.1 | AB051C | Frequency data 1 |
| 877 | 40.4054 | +0.7 | AB111C | Memory channels |
| 878 | 40.4540 | +48.6 | AB0A1C | Unknown (memory?) |
| 879 | 40.4552 | +1.2 | AB0410 | Battery level |

**Total: 6 packets in 0.29 seconds**

---

## Sequence 3: Second Polling Command (Frame 882, t=41.230521s)

### APP SENDS: AB020C (Status Request/Poll)
**Frame 882** | **Time: 41.230521s** | **Handle: 0x000c** | **Direction: SENT**  
```
Value: AB020C
Interval from previous command: 1.179s
```

### RADIO RESPONDS: Status Burst (102ms latency)
**Response begins at t=41.332505s (+0.101984s)**

| Frame | Time (s) | Δt (ms) | Packet | Description |
|-------|----------|---------|--------|-------------|
| 885 | 41.3325 | +0.0 | AB0901 | Squelch status |
| 886 | 41.5285 | +196.0 | AB071C | Scan status |
| 887 | 41.5771 | +48.6 | AB051C | Frequency data 1 |
| 888 | 41.6267 | +49.6 | AB0E1C | Frequency details |
| 889 | 41.6275 | +0.8 | AB0410 | Battery level |

**Total: 5 packets in 0.30 seconds**

---

## Sequence 4: Third Polling Command (Frame 891, t=42.466098s)

### APP SENDS: AB020C (Status Request/Poll)
**Frame 891** | **Time: 42.466098s** | **Handle: 0x000c** | **Direction: SENT**  
```
Value: AB020C
Interval from previous command: 1.236s
```

### RADIO RESPONDS: Status Burst (116ms latency)
**Response begins at t=42.582197s (+0.116099s)**

| Frame | Time (s) | Δt (ms) | Packet | Description |
|-------|----------|---------|--------|-------------|
| 894 | 42.5822 | +0.0 | AB0901 | Squelch status |
| 895 | 42.7788 | +196.6 | AB071C | Scan status |
| 896 | 42.8274 | +48.6 | AB071C | Scan status (repeat) |
| 897 | 42.8762 | +48.8 | AB0E1C | Frequency details |
| 898 | 42.8770 | +0.8 | AB0410 | Battery level |
| 899 | 42.9248 | +47.8 | AB051C | Frequency data 1 |
| 900 | 42.9734 | +48.6 | AB061C | Frequency data 2 |

**Total: 7 packets in 0.39 seconds**

---

## Pattern Summary

### Handshake Response Pattern (AB01FF)
- **Latency:** ~100ms
- **Packet count:** 30+ packets
- **Duration:** ~1 second
- **Characteristics:**
  - Sends one of each packet type
  - AB1119 repeated 4 times (memory banks?)
  - Includes detailed variants (AB0506 vs AB051C, AB0417 vs AB0410)
  - Complete radio state dump

### Poll Response Pattern (AB020C)
- **Latency:** 100-120ms
- **Packet count:** 4-7 packets per poll
- **Duration:** 0.25-0.40 seconds
- **Characteristics:**
  - Always starts with AB0901 (squelch)
  - Always ends with AB0410 (battery)
  - Variable middle packets based on radio state
  - Shorter variants of packets (AB051C not AB0506)

### Response Timing
- **Inter-packet delay:** 0.5-50ms (very fast burst)
- **Typical burst pattern:** 
  - Immediate: AB0901 (+0ms)
  - Delayed: Next packets (+190-200ms)
  - Rapid: Remaining packets (+0.5-50ms each)
- **Command interval:** 1.0-1.5 seconds between polls

### Autonomous Updates
Between polling commands, radio continuously sends:
- **AB061C** at ~1 second intervals
- No command required (autonomous notification)
- Appears to track frequency changes or signal detection

---

## Command Types Observed

| Command | Hex | When Used | Expected Response |
|---------|-----|-----------|-------------------|
| Handshake | AB01FF | Once at startup | 30+ packets (complete dump) |
| Poll/Status | AB020C | Every 1-2 seconds | 4-7 packets (current state) |
| (Unknown) | AB??XX | Not observed | - |

---

## Protocol Behavior Patterns

### 1. Two-Stage Initialization
```
1. BLE Connection → Radio sends AB061C autonomously
2. App sends AB01FF → Radio dumps complete state
3. App begins AB020C polling → Radio sends updates
```

### 2. Response Burst Pattern
```
Command received
  ↓ (100ms)
First packet (AB0901)
  ↓ (200ms)
Burst of 3-6 more packets
  ↓ (50ms each)
Complete
```

### 3. Packet Prioritization
Radio appears to send in priority order:
1. **Immediate:** Squelch status (AB0901)
2. **Delayed:** Other status packets
3. **Grouped:** Related data sent together (AB051C + AB061C)

### 4. State-Dependent Responses
Poll responses vary based on radio state:
- **Scanning:** Includes AB071C (scan status)
- **Signal active:** Includes AB0E1C (frequency details)
- **Idle:** Minimal response (squelch + battery only)

---

## Timing Considerations for Implementation

### Recommended Timeouts
```java
// Command timeout (wait for first response)
COMMAND_TIMEOUT = 250ms;  // Radio responds in 100-120ms

// Response burst timeout (wait for all packets)
BURST_TIMEOUT = 500ms;  // All packets arrive within 400ms

// Polling interval
POLL_INTERVAL = 1200ms;  // Observed: 1.0-1.5 seconds
```

### Processing Strategy
```java
1. Send command
2. Start timeout timer (250ms)
3. Collect all packets until 500ms silence
4. Process batch as single update
5. Wait POLL_INTERVAL before next command
```

---

**Document Generated:** Based on BIDIRECTIONAL_CAPTURE.txt analysis  
**Source:** BTDump.pdml (600 AB protocol packets)  
**Analysis Focus:** Command-response timing and sequencing
