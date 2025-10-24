package com.myhomesmartlife.bluetooth.CleanedUp;

import android.util.Log;

/**
 * Radio Protocol Data Handler
 * 
 * Handles parsing of incoming data packets from the radio device.
 * The radio sends various status updates and responses via Bluetooth.
 * 
 * Packet Format:
 * - Bytes 0-1: Start header (0xAB)
 * - Bytes 2-3: Length indicator
 * - Bytes 4-5: Command type identifier
 * - Remaining bytes: Data payload
 * 
 * Common Command Types (in hex string format):
 * - "ab0417": Frequency and status update (main data packet)
 * - "ab031e": Time/clock update
 * - "ab0901": Band and channel information
 * - "ab0303": Volume level
 * - "ab031f": Signal strength
 * - "ab090f": Frequency input mode
 */
public class RadioProtocolHandler {
    
    private static final String TAG = "RadioProtocolHandler";
    
    // ==================== PROTOCOL CONSTANTS ====================
    
    /** Protocol start byte in hex string format */
    public static final String PROTOCOL_START_HEX = "ab";
    
    /** Minimum packet length for valid commands */
    public static final int MIN_PACKET_LENGTH = 6;
    
    /** Command identifier length (first 6 hex characters) */
    public static final int COMMAND_ID_LENGTH = 6;
    
    /** Minimum length for frequency status packets */
    public static final int MIN_FREQ_STATUS_LENGTH = 12;
    
    /** Minimum length for band info packets */
    public static final int MIN_BAND_INFO_LENGTH = 32;
    
    /** Minimum length for standard status packets */
    public static final int MIN_STATUS_LENGTH = 16;
    
    
    // ==================== COMMAND TYPE IDENTIFIERS ====================
    
    /** Main frequency and status packet */
    public static final String CMD_TYPE_FREQUENCY_STATUS = "ab0417";
    
    /** Time/clock packet */
    public static final String CMD_TYPE_TIME = "ab031e";
    
    /** Band information packet */
    public static final String CMD_TYPE_BAND_INFO = "ab0901";
    
    /** Volume level packet */
    public static final String CMD_TYPE_VOLUME = "ab0303";
    
    /** Signal strength packet */
    public static final String CMD_TYPE_SIGNAL = "ab031f";
    
    /** Frequency input mode packet */
    public static final String CMD_TYPE_FREQ_INPUT = "ab090f";
    
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
    
    /** Status/mode packet (short) */
    public static final String CMD_TYPE_STATUS_SHORT = "ab02";
    
    /** Frequency data part 1 (paired with AB06) */
    public static final String CMD_TYPE_FREQ_DATA_1 = "ab05";
    
    /** Frequency data part 2 (paired with AB05) - ASCII channel/frequency */
    public static final String CMD_TYPE_FREQ_DATA_2 = "ab06";
    
    /** Battery/extended status packet */
    public static final String CMD_TYPE_BATTERY = "ab07";
    
    /** Detailed frequency/demodulation info */
    public static final String CMD_TYPE_DETAILED_FREQ = "ab09";
    
    /** Bandwidth information packet (ASCII) */
    public static final String CMD_TYPE_BANDWIDTH = "ab0d";
    
    
    // ==================== DATA STRUCTURES ====================
    
    /**
     * Parsed radio status data
     */
    public static class RadioStatus {
        public String frequency;        // Current frequency in Hz (as string)
        public String band;             // Band identifier (e.g., "06" for VHF)
        public String subBand;          // Sub-band information
        public String demodulation;     // Demodulation mode
        public String bandwidth;        // Bandwidth setting
        public int squelchLevel;        // Squelch level (0-15)
        public int volumeLevel;         // Volume level (0-15)
        public boolean isStereo;        // Stereo mode enabled
        public boolean isPowerOn;       // Power state
        public String rawData;          // Raw hex data for debugging
        
        @Override
        public String toString() {
            return "RadioStatus{" +
                    "frequency='" + frequency + '\'' +
                    ", band='" + band + '\'' +
                    ", squelchLevel=" + squelchLevel +
                    ", volumeLevel=" + volumeLevel +
                    ", isStereo=" + isStereo +
                    ", isPowerOn=" + isPowerOn +
                    '}';
        }
    }
    
    /**
     * Listener interface for parsed radio data
     */
    public interface RadioDataListener {
        void onFrequencyChanged(String frequency, String band);
        void onVolumeChanged(int volume);
        void onSignalStrengthChanged(int strength);
        void onStatusUpdate(RadioStatus status);
        void onDeviceInfo(String deviceInfo);
        void onLockStatusChanged(boolean isLocked);
        void onRecordingStatusChanged(boolean isRecording, int recordIndex);
        void onBatteryLevel(int batteryPercent);
        void onChannelDisplay(String channelText);
    }
    
    
    // ==================== MEMBER VARIABLES ====================
    
    private RadioDataListener dataListener;
    private StringBuilder deviceInfoBuffer = new StringBuilder();
    private String lastFreqData1 = null; // Buffer for AB05 packets
    
    
    // ==================== PARSING METHODS ====================
    
    /**
     * Parse incoming data from radio
     * 
     * @param data Raw byte data received
     */
    public void parseReceivedData(byte[] data) {
        if (data == null || data.length < MIN_PACKET_LENGTH) {
            Log.w(TAG, "Invalid data packet received");
            return;
        }
        
        // Convert to hex string for easier parsing
        String hexString = bytesToHexString(data);
        Log.d(TAG, "Parsing data: " + hexString);
        
        // Extract command identifier (first 6 characters)
        String commandId = hexString.substring(0, Math.min(COMMAND_ID_LENGTH, hexString.length()));
        
        // Route to appropriate parser based on command type
        switch (commandId) {
            case CMD_TYPE_FREQUENCY_STATUS:
                parseFrequencyStatus(hexString);
                break;
                
            case CMD_TYPE_TIME:
                parseTimeUpdate(hexString);
                break;
                
            case CMD_TYPE_BAND_INFO:
                parseBandInfo(hexString);
                break;
                
            case CMD_TYPE_VOLUME:
                parseVolumeLevel(hexString);
                break;
                
            case CMD_TYPE_SIGNAL:
                parseSignalStrength(hexString);
                break;
                
            case CMD_TYPE_FREQ_INPUT:
                parseFrequencyInput(hexString);
                break;
                
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
                
            case CMD_TYPE_STATUS_SHORT:
                parseStatusShort(hexString);
                break;
                
            case CMD_TYPE_FREQ_DATA_1:
                parseFreqData1(hexString);
                break;
                
            case CMD_TYPE_FREQ_DATA_2:
                parseFreqData2(hexString);
                break;
                
            case CMD_TYPE_BATTERY:
                parseBattery(hexString);
                break;
                
            case CMD_TYPE_DETAILED_FREQ:
                parseDetailedFreq(hexString);
                break;
                
            case CMD_TYPE_BANDWIDTH:
                parseBandwidth(hexString);
                break;
                
            default:
                Log.d(TAG, "Unknown command type: " + commandId);
                break;
        }
    }
    
    /**
     * Parse frequency and status packet (ab0417)
     * 
     * Format:
     * Bytes 0-5: Header "ab0417"
     * Bytes 6-7: Byte 1 (flags/status)
     * Bytes 8-9: Byte 2 (flags/status)
     * Bytes 10-11: Byte 3 (flags/status)
     * Bytes 12-19: Frequency data (4 bytes)
     * 
     * @param hexData Hex string data
     */
    private void parseFrequencyStatus(String hexData) {
        if (hexData.length() < MIN_FREQ_STATUS_LENGTH) {
            Log.w(TAG, "Frequency status packet too short");
            return;
        }
        
        try {
            // Extract status bytes
            String byte1 = hexData.substring(6, 8);
            String byte2 = hexData.substring(8, 10);
            String byte3 = hexData.substring(10, 12);
            
            Log.d(TAG, String.format("Status bytes: %s %s %s", byte1, byte2, byte3));
            
            // Create status object
            RadioStatus status = new RadioStatus();
            status.rawData = hexData;
            
            // Notify listener
            if (dataListener != null) {
                dataListener.onStatusUpdate(status);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing frequency status", e);
        }
    }
    
    /**
     * Parse band information packet (ab0901)
     * 
     * Format:
     * Bytes 0-5: Header "ab0901"
     * Bytes 6-7: Band code
     * Bytes 8-9: Sub-band 1
     * Bytes 10-11: Sub-band 2
     * Bytes 12-13: Sub-band 3
     * Bytes 14-15: Sub-band 4
     * Bytes 16-31: Frequency components (8 bytes total)
     * 
     * @param hexData Hex string data
     */
    private void parseBandInfo(String hexData) {
        if (hexData.length() < MIN_BAND_INFO_LENGTH) {
            Log.w(TAG, "Band info packet too short");
            return;
        }
        
        try {
            // Extract band identifier
            String bandCode = hexData.substring(6, 8);
            
            // Extract sub-bands
            String subBand1 = hexData.substring(8, 10);
            String subBand2 = hexData.substring(10, 12);
            String subBand3 = hexData.substring(12, 14);
            String subBand4 = hexData.substring(14, 16);
            
            // Extract frequency bytes (positions 16-31, 4 groups of 2 bytes each)
            String freqByte1 = hexData.substring(16, 18);
            String freqByte2 = hexData.substring(18, 20);
            String freqByte3 = hexData.substring(20, 22);
            String freqByte4 = hexData.substring(22, 24);
            
            // Combine frequency bytes (little endian)
            String frequencyHex = freqByte4 + freqByte3 + freqByte2 + freqByte1;
            String frequency = hexToDec(frequencyHex);
            
            Log.d(TAG, String.format("Band: %s, Frequency: %s Hz", bandCode, frequency));
            
            if (dataListener != null) {
                dataListener.onFrequencyChanged(frequency, bandCode);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing band info", e);
        }
    }
    
    /**
     * Parse volume level packet (ab0303)
     * 
     * @param hexData Hex string data
     */
    private void parseVolumeLevel(String hexData) {
        if (hexData.length() < 10) {
            return;
        }
        
        try {
            String volumeHex = hexData.substring(6, 8);
            int volume = Integer.parseInt(volumeHex, 16);
            
            Log.d(TAG, "Volume: " + volume);
            
            if (dataListener != null) {
                dataListener.onVolumeChanged(volume);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing volume", e);
        }
    }
    
    /**
     * Parse signal strength packet (ab031f)
     * 
     * @param hexData Hex string data
     */
    private void parseSignalStrength(String hexData) {
        if (hexData.length() < 10) {
            return;
        }
        
        try {
            String signalHex = hexData.substring(6, 8);
            int strength = Integer.parseInt(signalHex, 16);
            
            Log.d(TAG, "Signal strength: " + strength);
            
            if (dataListener != null) {
                dataListener.onSignalStrengthChanged(strength);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing signal strength", e);
        }
    }
    
    /**
     * Parse time update packet (ab031e)
     * 
     * @param hexData Hex string data
     */
    private void parseTimeUpdate(String hexData) {
        Log.d(TAG, "Time update received: " + hexData);
        // Time parsing can be implemented based on specific requirements
    }
    
    /**
     * Parse frequency input mode packet (ab090f)
     * 
     * @param hexData Hex string data
     */
    private void parseFrequencyInput(String hexData) {
        Log.d(TAG, "Frequency input mode: " + hexData);
        // Parse frequency input state
    }
    
    /**
     * Parse device info packet (ab11/ab10)
     * Multi-part ASCII message containing device version, model, contact info
     * 
     * Format: AB11/AB10 [LENGTH] [SEQUENCE] [DATA_LENGTH] [ASCII_TEXT...] [CHECKSUM]
     * 
     * @param hexData Hex string data
     */
    private void parseDeviceInfo(String hexData) {
        if (hexData.length() < 14) {
            Log.w(TAG, "Device info packet too short");
            return;
        }
        
        try {
            int length = Integer.parseInt(hexData.substring(2, 4), 16);
            int sequence = Integer.parseInt(hexData.substring(4, 6), 16);
            int dataLength = Integer.parseInt(hexData.substring(6, 8), 16);
            
            // Extract ASCII text (dataLength characters = dataLength*2 hex digits)
            int textStartPos = 8;
            int textEndPos = textStartPos + (dataLength * 2);
            
            if (hexData.length() < textEndPos) {
                Log.w(TAG, "Device info data truncated");
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String text = hexToAscii(textHex);
            
            // Accumulate multi-part message
            deviceInfoBuffer.append(text);
            
            Log.d(TAG, "Device info part " + sequence + ": \"" + text + "\"");
            
            // Check if this appears to be the last part
            // (contains email address end or specific patterns)
            if (text.contains(".com") || text.contains(".net") || 
                hexData.startsWith("ab10")) { // ab10 often marks end
                
                String completeInfo = deviceInfoBuffer.toString();
                Log.i(TAG, "Complete device info:\n" + completeInfo);
                
                if (dataListener != null) {
                    dataListener.onDeviceInfo(completeInfo);
                }
                
                // Clear buffer for next message
                deviceInfoBuffer.setLength(0);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing device info", e);
        }
    }
    
    /**
     * Parse sub-band info packet (ab0e)
     * Contains sub-band name in ASCII
     * 
     * Format: AB0E [LENGTH] [INDEX] [MARKER] [NAME_LENGTH] [ASCII_NAME...] [CHECKSUM]
     * 
     * @param hexData Hex string data
     */
    private void parseSubBandInfo(String hexData) {
        if (hexData.length() < 16) {
            Log.w(TAG, "Sub-band info packet too short");
            return;
        }
        
        try {
            int subBandIndex = Integer.parseInt(hexData.substring(4, 6), 16);
            int textLength = Integer.parseInt(hexData.substring(6, 8), 16);
            
            int textStartPos = 8;
            int textEndPos = textStartPos + (textLength * 2);
            
            if (hexData.length() < textEndPos) {
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String subBandName = hexToAscii(textHex);
            
            Log.d(TAG, "Sub-band " + subBandIndex + ": \"" + subBandName.trim() + "\"");
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing sub-band info", e);
        }
    }
    
    /**
     * Parse lock status packet (ab08)
     * Indicates whether keypad/controls are locked
     * 
     * Format: AB08 [LENGTH] [LOCK_TYPE] [MARKER] [TEXT_LENGTH] [ASCII_STATUS] [CHECKSUM]
     * 
     * @param hexData Hex string data
     */
    private void parseLockStatus(String hexData) {
        if (hexData.length() < 16) {
            Log.w(TAG, "Lock status packet too short");
            return;
        }
        
        try {
            int lockType = Integer.parseInt(hexData.substring(4, 6), 16);
            int textLength = Integer.parseInt(hexData.substring(6, 8), 16);
            
            int textStartPos = 8;
            int textEndPos = textStartPos + (textLength * 2);
            
            if (hexData.length() < textEndPos) {
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String status = hexToAscii(textHex);
            
            boolean isLocked = status.toUpperCase().contains("LOCK");
            
            Log.d(TAG, "Lock status: \"" + status + "\" (locked=" + isLocked + ")");
            
            if (dataListener != null) {
                dataListener.onLockStatusChanged(isLocked);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing lock status", e);
        }
    }
    
    /**
     * Parse recording status packet (ab0b)
     * Shows current recording state
     * 
     * Format: AB0B [LENGTH] [REC_INDEX] [MARKER] [TEXT_LENGTH] [ASCII_STATUS] [CHECKSUM]
     * 
     * @param hexData Hex string data
     */
    private void parseRecordingStatus(String hexData) {
        if (hexData.length() < 16) {
            Log.w(TAG, "Recording status packet too short");
            return;
        }
        
        try {
            int recordIndex = Integer.parseInt(hexData.substring(4, 6), 16);
            int textLength = Integer.parseInt(hexData.substring(6, 8), 16);
            
            int textStartPos = 8;
            int textEndPos = textStartPos + (textLength * 2);
            
            if (hexData.length() < textEndPos) {
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String status = hexToAscii(textHex);
            
            // Recording is active if status doesn't contain "OFF"
            boolean isRecording = !status.toUpperCase().contains("OFF");
            
            Log.d(TAG, "Recording slot " + recordIndex + ": \"" + status + 
                       "\" (active=" + isRecording + ")");
            
            if (dataListener != null) {
                dataListener.onRecordingStatusChanged(isRecording, recordIndex);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing recording status", e);
        }
    }
    
    
    // ==================== UTILITY METHODS ====================
    
    /**
     * Convert byte array to hex string
     * 
     * @param bytes Byte array
     * @return Hex string (lowercase)
     */
    public static String bytesToHexString(byte[] bytes) {
        if (bytes == null) {
            return "";
        }
        
        StringBuilder result = new StringBuilder();
        for (byte b : bytes) {
            result.append(String.format("%02x", b & 0xFF));
        }
        return result.toString();
    }
    
    /**
     * Convert hex string to decimal string
     * 
     * @param hexString Hex string
     * @return Decimal string representation
     */
    public static String hexToDec(String hexString) {
        try {
            long value = Long.parseLong(hexString, 16);
            return String.valueOf(value);
        } catch (NumberFormatException e) {
            Log.e(TAG, "Error converting hex to decimal: " + hexString, e);
            return "0";
        }
    }
    
    /**
     * Convert hex string to byte array
     * 
     * @param hexString Hex string (even length)
     * @return Byte array
     */
    public static byte[] hexStringToBytes(String hexString) {
        if (hexString == null || hexString.length() % 2 != 0) {
            return new byte[0];
        }
        
        int len = hexString.length();
        byte[] data = new byte[len / 2];
        
        for (int i = 0; i < len; i += 2) {
            data[i / 2] = (byte) ((Character.digit(hexString.charAt(i), 16) << 4)
                    + Character.digit(hexString.charAt(i + 1), 16));
        }
        
        return data;
    }
    
    /**
     * Extract nibbles from a byte
     * 
     * @param dataByte The byte to extract from
     * @return Array with [low nibble, high nibble]
     */
    public static int[] extractNibbles(byte dataByte) {
        int lowNibble = dataByte & 0x0F;          // Lower 4 bits
        int highNibble = (dataByte >> 4) & 0x0F;  // Upper 4 bits
        return new int[]{lowNibble, highNibble};
    }
    
    /**
     * Convert hex string to ASCII text
     * 
     * @param hexString Hex string (must be even length)
     * @return ASCII text
     */
    public static String hexToAscii(String hexString) {
        if (hexString == null || hexString.length() % 2 != 0) {
            return "";
        }
        
        StringBuilder output = new StringBuilder();
        for (int i = 0; i < hexString.length(); i += 2) {
            String str = hexString.substring(i, i + 2);
            int charCode = Integer.parseInt(str, 16);
            output.append((char) charCode);
        }
        return output.toString();
    }
    
    /**
     * Convert ASCII text to hex string
     * 
     * @param text ASCII text
     * @return Hex string
     */
    public static String asciiToHex(String text) {
        if (text == null) {
            return "";
        }
        
        StringBuilder hex = new StringBuilder();
        for (char c : text.toCharArray()) {
            hex.append(String.format("%02X", (int) c));
        }
        return hex.toString();
    }
    
    /**
     * Parse status short packet (ab02)
     * Simple status/mode indicator
     * 
     * Format: AB02 [LENGTH] [STATUS] [CHECKSUM]
     * Example: AB022001CE (status=0x20)
     * 
     * @param hexData Hex string data
     */
    private void parseStatusShort(String hexData) {
        if (hexData.length() < 10) {
            return;
        }
        
        try {
            int status = Integer.parseInt(hexData.substring(6, 8), 16);
            Log.d(TAG, "Status short: 0x" + Integer.toHexString(status));
            // Status values observed: 0x20 (normal), 0x05 (mode change), 0x07 (battery update)
        } catch (Exception e) {
            Log.e(TAG, "Error parsing status short", e);
        }
    }
    
    /**
     * Parse frequency data part 1 (ab05)
     * First part of paired AB05/AB06 message sequence
     * Contains frequency/mode index
     * 
     * Format: AB05 [LENGTH] [INDEX1] [INDEX2] [MODE] [CHECKSUM]
     * Example: AB051C0603013107 (index=0x1C06, mode=0x31)
     * 
     * @param hexData Hex string data
     */
    private void parseFreqData1(String hexData) {
        if (hexData.length() < 14) {
            return;
        }
        
        try {
            int index1 = Integer.parseInt(hexData.substring(4, 6), 16);
            int index2 = Integer.parseInt(hexData.substring(6, 8), 16);
            int mode = Integer.parseInt(hexData.substring(10, 12), 16);
            
            // Store for pairing with AB06
            lastFreqData1 = hexData;
            
            Log.d(TAG, String.format("Freq data 1: index=0x%02X%02X mode=0x%02X", 
                                     index1, index2, mode));
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing freq data 1", e);
        }
    }
    
    /**
     * Parse frequency data part 2 (ab06)
     * Second part of paired AB05/AB06 message sequence
     * Contains ASCII channel/frequency text
     * 
     * Format: AB06 [LENGTH] [INDEX1] [INDEX2] [TEXT_LENGTH] [ASCII_TEXT...] [CHECKSUM]
     * Example: AB061C08030233313E → "31" (channel 31)
     * 
     * @param hexData Hex string data
     */
    private void parseFreqData2(String hexData) {
        if (hexData.length() < 16) {
            return;
        }
        
        try {
            int index1 = Integer.parseInt(hexData.substring(4, 6), 16);
            int index2 = Integer.parseInt(hexData.substring(6, 8), 16);
            int textLength = Integer.parseInt(hexData.substring(8, 10), 16);
            
            int textStartPos = 10;
            int textEndPos = textStartPos + (textLength * 2);
            
            if (hexData.length() < textEndPos) {
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String channelText = hexToAscii(textHex);
            
            Log.d(TAG, String.format("Freq data 2: index=0x%02X%02X text=\"%s\"", 
                                     index1, index2, channelText));
            
            if (dataListener != null) {
                dataListener.onChannelDisplay(channelText.trim());
            }
            
            // Clear paired data
            lastFreqData1 = null;
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing freq data 2", e);
        }
    }
    
    /**
     * Parse battery packet (ab07)
     * Contains battery level or extended status
     * 
     * Format: AB07 [LENGTH] [STATUS] [BATTERY] [CHECKSUM]
     * Example: AB020702B6 (battery at position 6-7)
     * 
     * @param hexData Hex string data
     */
    private void parseBattery(String hexData) {
        if (hexData.length() < 10) {
            return;
        }
        
        try {
            int batteryValue = Integer.parseInt(hexData.substring(6, 8), 16);
            
            // Battery level interpretation (observed values: 0x02 = low, 0x07 = full?)
            // May need calibration based on actual device behavior
            int batteryPercent = (batteryValue * 100) / 7; // Rough estimate
            batteryPercent = Math.min(100, Math.max(0, batteryPercent));
            
            Log.d(TAG, "Battery: " + batteryPercent + "% (raw=0x" + 
                       Integer.toHexString(batteryValue) + ")");
            
            if (dataListener != null) {
                dataListener.onBatteryLevel(batteryPercent);
            }
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing battery", e);
        }
    }
    
    /**
     * Parse detailed frequency info (ab09)
     * Extended frequency/demodulation information
     * 
     * Format: AB09 [LENGTH] [INDEX] [MODE] [FREQ_BYTES...] [CHECKSUM]
     * Example: AB090106927A0200001300DC
     *   - Index: 0x01
     *   - Mode: 0x06
     *   - Frequency data: 927A020000
     *   - Additional: 0x13 (squelch?)
     * 
     * @param hexData Hex string data
     */
    private void parseDetailedFreq(String hexData) {
        if (hexData.length() < 20) {
            return;
        }
        
        try {
            int index = Integer.parseInt(hexData.substring(4, 6), 16);
            int mode = Integer.parseInt(hexData.substring(6, 8), 16);
            
            // Extract frequency bytes (6 bytes = 12 hex chars)
            String freqHex = hexData.substring(8, 20);
            
            // Additional parameter at position 20-21 (often squelch or filter setting)
            int param = 0;
            if (hexData.length() >= 22) {
                param = Integer.parseInt(hexData.substring(20, 22), 16);
            }
            
            Log.d(TAG, String.format("Detailed freq: idx=0x%02X mode=0x%02X freq=%s param=0x%02X",
                                     index, mode, freqHex, param));
            
            // Note: Frequency decoding may require specific interpretation
            // based on the radio's encoding scheme
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing detailed freq", e);
        }
    }
    
    /**
     * Parse bandwidth info packet (ab0d)
     * Contains bandwidth setting in ASCII
     * 
     * Format: AB0D [LENGTH] [INDEX1] [INDEX2] [TEXT_LENGTH] [ASCII_TEXT...] [CHECKSUM]
     * Example: AB0D1C03030942616E64576964746858 → "BandWidth"
     * 
     * @param hexData Hex string data
     */
    private void parseBandwidth(String hexData) {
        if (hexData.length() < 16) {
            return;
        }
        
        try {
            int index1 = Integer.parseInt(hexData.substring(4, 6), 16);
            int index2 = Integer.parseInt(hexData.substring(6, 8), 16);
            int textLength = Integer.parseInt(hexData.substring(8, 10), 16);
            
            int textStartPos = 10;
            int textEndPos = textStartPos + (textLength * 2);
            
            if (hexData.length() < textEndPos) {
                return;
            }
            
            String textHex = hexData.substring(textStartPos, textEndPos);
            String bandwidthText = hexToAscii(textHex);
            
            Log.d(TAG, String.format("Bandwidth: index=0x%02X%02X text=\"%s\"", 
                                     index1, index2, bandwidthText));
            
        } catch (Exception e) {
            Log.e(TAG, "Error parsing bandwidth", e);
        }
    }
    
    
    // ==================== GETTERS & SETTERS ====================
    
    public void setDataListener(RadioDataListener listener) {
        this.dataListener = listener;
    }
    
    public RadioDataListener getDataListener() {
        return dataListener;
    }
}
