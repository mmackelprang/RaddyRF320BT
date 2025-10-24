package com.myhomesmartlife.bluetooth.CleanedUp;

/**
 * Radio Protocol Command Constants
 * 
 * This class contains all the Bluetooth command byte arrays used to communicate
 * with the radio device. Commands follow a specific protocol format:
 * 
 * Format: [START_BYTE, LENGTH, COMMAND_TYPE, DATA..., CHECKSUM]
 * - START_BYTE: 0xAB (171 or -85 in signed byte)
 * - LENGTH: Total message length indicator
 * - COMMAND_TYPE: Type of command (e.g., COMMAND_TYPE_BUTTON for button presses)
 * - DATA: Command-specific data
 * - CHECKSUM: Simple checksum for data integrity
 * 
 * All commands use 5 bytes in this implementation.
 */
public class RadioProtocolCommands {
    
    // ==================== PROTOCOL CONSTANTS ====================
    
    /** Protocol start byte (0xAB = -85 signed) */
    public static final byte PROTOCOL_START_BYTE = (byte) 0xAB;
    
    /** Standard message length for button commands */
    public static final byte MESSAGE_LENGTH_STANDARD = 0x02;
    
    /** Message length for handshake commands */
    public static final byte MESSAGE_LENGTH_HANDSHAKE = 0x01;
    
    /** Standard command type for button presses */
    public static final byte COMMAND_TYPE_BUTTON = 0x0C;
    
    /** Command type for handshake/acknowledgment */
    public static final byte COMMAND_TYPE_HANDSHAKE = 0x01;
    
    /** Command type for acknowledgment response */
    public static final byte COMMAND_TYPE_ACK = 0x12;
    
    
    // ==================== COMMON DATA BYTES ====================
    
    /** Handshake data byte */
    public static final byte DATA_HANDSHAKE = (byte) 0xFF;
    
    /** Success response data */
    public static final byte DATA_SUCCESS = 0x01;
    
    /** Failure response data */
    public static final byte DATA_FAILURE = 0x00;
    
    /** Number button data bytes */
    public static final byte DATA_BUTTON_0 = 0x0A;  // Number 0
    public static final byte DATA_BUTTON_1 = 0x01;  // Number 1
    public static final byte DATA_BUTTON_2 = 0x02;  // Number 2
    public static final byte DATA_BUTTON_3 = 0x03;  // Number 3
    public static final byte DATA_BUTTON_4 = 0x04;  // Number 4
    public static final byte DATA_BUTTON_5 = 0x05;  // Number 5
    public static final byte DATA_BUTTON_6 = 0x06;  // Number 6
    public static final byte DATA_BUTTON_7 = 0x07;  // Number 7
    public static final byte DATA_BUTTON_8 = 0x08;  // Number 8
    public static final byte DATA_BUTTON_9 = 0x09;  // Number 9
    
    /** Band and navigation button data bytes */
    public static final byte DATA_BAND = 0x00;       // Band button
    public static final byte DATA_SUB_BAND = 0x17;   // Sub-band button
    public static final byte DATA_BACK = 0x0B;       // Back/Return button
    public static final byte DATA_POINT = 0x0C;      // Point/Decimal button
    public static final byte DATA_FREQ = 0x0D;       // Frequency button
    public static final byte DATA_UP_SHORT = 0x0E;   // Up button short
    public static final byte DATA_UP_LONG = 0x0F;    // Up button long
    public static final byte DATA_DOWN_SHORT = 0x10; // Down button short
    public static final byte DATA_DOWN_LONG = 0x11;  // Down button long
    
    /** Volume control data bytes */
    public static final byte DATA_VOLUME_UP = 0x12;   // Volume up
    public static final byte DATA_VOLUME_DOWN = 0x13; // Volume down
    
    /** Power and system data bytes */
    public static final byte DATA_POWER = 0x14;       // Power button
    public static final byte DATA_BLUETOOTH = 0x1C;  // Bluetooth button
    
    
    // ==================== HANDSHAKE & CONNECTION ====================
    
    /** Handshake command sent on connection establishment */
    public static final byte[] CMD_HANDSHAKE = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_HANDSHAKE, DATA_HANDSHAKE, PROTOCOL_START_BYTE
    };
    
    /** Acknowledgment success response */
    public static final byte[] CMD_ACK_SUCCESS = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_ACK, DATA_SUCCESS, (byte) 0xC0
    };
    
    /** Acknowledgment failure response */
    public static final byte[] CMD_ACK_FAILURE = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_ACK, DATA_FAILURE, (byte) 0xBF
    };
    
    
    // ==================== BAND CONTROL ====================
    
    /** Band selection button */
    public static final byte[] CMD_BAND = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, DATA_BAND, (byte) 0xB9
    };
    
    /** Sub-band selection button */
    public static final byte[] CMD_SUB_BAND = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, DATA_SUB_BAND, (byte) 0xD0
    };
    
    /** Band button long press */
    public static final byte[] CMD_BAND_LONG_PRESS = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x29, (byte) 0xE2
    };
    
    
    // ==================== NUMBER BUTTONS (0-9) ====================
    
    /** Number 0 button */
    public static final byte[] CMD_NUMBER_0 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, DATA_BUTTON_0, (byte) 0xC3
    };
    
    /** Number 1 button */
    public static final byte[] CMD_NUMBER_1 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, DATA_BUTTON_1, (byte) 0xBA
    };
    
    /** Number 2 button */
    public static final byte[] CMD_NUMBER_2 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, DATA_BUTTON_2, (byte) 0xBB
    };
    
    /** Number 3 button */
    public static final byte[] CMD_NUMBER_3 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x03, (byte) 0xBC
    };
    
    /** Number 4 button */
    public static final byte[] CMD_NUMBER_4 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x04, (byte) 0xBD
    };
    
    /** Number 5 button */
    public static final byte[] CMD_NUMBER_5 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x05, (byte) 0xBE
    };
    
    /** Number 6 button */
    public static final byte[] CMD_NUMBER_6 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x06, (byte) 0xBF
    };
    
    /** Number 7 button */
    public static final byte[] CMD_NUMBER_7 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x07, (byte) 0xC0
    };
    
    /** Number 8 button */
    public static final byte[] CMD_NUMBER_8 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x08, (byte) 0xC1
    };
    
    /** Number 9 button */
    public static final byte[] CMD_NUMBER_9 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x09, (byte) 0xC2
    };
    
    
    // ==================== NUMBER BUTTONS LONG PRESS ====================
    
    /** Number 1 long press */
    public static final byte[] CMD_NUMBER_1_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x35, (byte) 0xEE
    };
    
    /** Number 2 long press */
    public static final byte[] CMD_NUMBER_2_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x36, (byte) 0xEF
    };
    
    /** Number 3 long press */
    public static final byte[] CMD_NUMBER_3_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x37, (byte) 0xF0
    };
    
    /** Number 4 long press */
    public static final byte[] CMD_NUMBER_4_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x38, (byte) 0xF1
    };
    
    /** Number 5 long press */
    public static final byte[] CMD_NUMBER_5_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x39, (byte) 0xF2
    };
    
    /** Number 6 long press */
    public static final byte[] CMD_NUMBER_6_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3A, (byte) 0xF3
    };
    
    /** Number 7 long press */
    public static final byte[] CMD_NUMBER_7_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3B, (byte) 0xF4
    };
    
    /** Number 8 long press */
    public static final byte[] CMD_NUMBER_8_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3C, (byte) 0xF5
    };
    
    /** Number 9 long press */
    public static final byte[] CMD_NUMBER_9_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3D, (byte) 0xF6
    };
    
    /** Number 0 long press (labeled as "TEN") */
    public static final byte[] CMD_NUMBER_0_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3E, (byte) 0xF7
    };
    
    
    // ==================== NAVIGATION BUTTONS ====================
    
    /** Back/Return button */
    public static final byte[] CMD_BACK = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x0B, (byte) 0xC4
    };
    
    /** Point/Decimal button */
    public static final byte[] CMD_POINT = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, COMMAND_TYPE_BUTTON, (byte) 0xC5
    };
    
    /** Frequency button */
    public static final byte[] CMD_FREQ = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x0D, (byte) 0xC6
    };
    
    /** Up button short click */
    public static final byte[] CMD_UP_SHORT = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x0E, (byte) 0xC7
    };
    
    /** Up button long click */
    public static final byte[] CMD_UP_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x0F, (byte) 0xC8
    };
    
    /** Down button short click */
    public static final byte[] CMD_DOWN_SHORT = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x10, (byte) 0xC9
    };
    
    /** Down button long click */
    public static final byte[] CMD_DOWN_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x11, (byte) 0xCA
    };
    
    
    // ==================== VOLUME CONTROL ====================
    
    /** Volume increase button */
    public static final byte[] CMD_VOLUME_UP = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x12, (byte) 0xCB
    };
    
    /** Volume decrease button */
    public static final byte[] CMD_VOLUME_DOWN = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x13, (byte) 0xCC
    };
    
    
    // ==================== POWER & SYSTEM ====================
    
    /** Power button */
    public static final byte[] CMD_POWER = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x14, (byte) 0xCD
    };
    
    /** Power button long press */
    public static final byte[] CMD_POWER_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x45, (byte) 0xFE
    };
    
    /** Bluetooth button */
    public static final byte[] CMD_BLUETOOTH = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1C, (byte) 0xD5
    };
    
    
    // ==================== AUDIO MODES ====================
    
    /** Music mode button */
    public static final byte[] CMD_MUSIC = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x26, (byte) 0xDF
    };
    
    /** Music mode long press */
    public static final byte[] CMD_MUSIC_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x3F, (byte) 0xF8
    };
    
    /** Play button */
    public static final byte[] CMD_PLAY = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1A, (byte) 0xD3
    };
    
    /** Play button long press */
    public static final byte[] CMD_PLAY_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x33, (byte) 0xEC
    };
    
    /** Play mode long press */
    public static final byte[] CMD_PLAY_MODE_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x40, (byte) 0xF9
    };
    
    /** Step/Skip button */
    public static final byte[] CMD_STEP = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1B, (byte) 0xD4
    };
    
    /** Step new version */
    public static final byte[] CMD_STEP_NEW = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2E, (byte) 0xE7
    };
    
    /** Circle/Loop button */
    public static final byte[] CMD_CIRCLE = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x27, (byte) 0xE0
    };
    
    /** Music type circle button */
    public static final byte[] CMD_MUSIC_TYPE_CIRCLE = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x28, (byte) 0xE1
    };
    
    
    // ==================== RADIO SETTINGS ====================
    
    /** Demodulation button */
    public static final byte[] CMD_DEMODULATION = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1D, (byte) 0xD6
    };
    
    /** Demodulation alternate command (same as CMD_DEMODULATION) */
    public static final byte[] CMD_DEMODULATION_ALT = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1D, (byte) 0xD6
    };
    
    /** Bandwidth button */
    public static final byte[] CMD_BANDWIDTH = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1E, (byte) 0xD7
    };
    
    /** Mobile display button */
    public static final byte[] CMD_MOBILE_DISPLAY = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1F, (byte) 0xD8
    };
    
    /** Squelch control button */
    public static final byte[] CMD_SQUELCH = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x20, (byte) 0xD9
    };
    
    /** Stereo button */
    public static final byte[] CMD_STEREO = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x21, (byte) 0xDA
    };
    
    /** De-emphasis control button */
    public static final byte[] CMD_DE_EMPHASIS = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x22, (byte) 0xDB
    };
    
    
    // ==================== MEMORY & PRESET ====================
    
    /** Preset button */
    public static final byte[] CMD_PRESET = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x23, (byte) 0xDC
    };
    
    /** Memo/Memory button */
    public static final byte[] CMD_MEMO = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x24, (byte) 0xDD
    };
    
    /** Memo long press */
    public static final byte[] CMD_MEMO_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2C, (byte) 0xE5
    };
    
    /** Memo/Meter long press */
    public static final byte[] CMD_METER_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x49, (byte) 0x02
    };
    
    
    // ==================== RECORDING ====================
    
    /** REC (Record) button */
    public static final byte[] CMD_REC = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x25, (byte) 0xDE
    };
    
    /** REC click */
    public static final byte[] CMD_REC_CLICK = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2D, (byte) 0xE6
    };
    
    
    // ==================== EMERGENCY & SPECIAL FUNCTIONS ====================
    
    /** SOS button */
    public static final byte[] CMD_SOS = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2A, (byte) 0xE3
    };
    
    /** SOS long press */
    public static final byte[] CMD_SOS_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2B, (byte) 0xE4
    };
    
    /** Alarm button click */
    public static final byte[] CMD_ALARM_CLICK = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x31, (byte) 0xEA
    };
    
    /** Alarm button long press */
    public static final byte[] CMD_ALARM_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x32, (byte) 0xEB
    };
    
    
    // ==================== FUNCTION KEYS ====================
    
    /** Function key long press */
    public static final byte[] CMD_FUNC_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x34, (byte) 0xED
    };
    
    /** Function key 1 */
    public static final byte[] CMD_FUNCTION_KEY_1 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1D, (byte) 0xD6
    };
    
    /** Function key 2 */
    public static final byte[] CMD_FUNCTION_KEY_2 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x1E, (byte) 0xD7
    };
    
    /** Function key 3 */
    public static final byte[] CMD_FUNCTION_KEY_3 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2E, (byte) 0xE7
    };
    
    /** Function key 4 */
    public static final byte[] CMD_FUNCTION_KEY_4 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x2F, (byte) 0xE8
    };
    
    /** Function key 5 */
    public static final byte[] CMD_FUNCTION_KEY_5 = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x30, (byte) 0xE9
    };
    
    
    // ==================== NUMERIC KEYPAD SPECIAL FUNCTIONS ====================
    
    /** Numeric mode long press */
    public static final byte[] CMD_NUMERIC_MODE_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x41, (byte) 0xFA
    };
    
    /** Equals button long press */
    public static final byte[] CMD_EQUALS_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x42, (byte) 0xFB
    };
    
    /** Minus button long press */
    public static final byte[] CMD_MINUS_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x43, (byte) 0xFC
    };
    
    /** Plus button long press */
    public static final byte[] CMD_PLUS_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x44, (byte) 0xFD
    };
    
    /** Enter button long press */
    public static final byte[] CMD_ENTER_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x46, (byte) 0xFF
    };
    
    /** Point/Decimal long press */
    public static final byte[] CMD_POINT_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x47, 0x00
    };
    
    /** Delete button long press */
    public static final byte[] CMD_DELETE_LONG = {
        PROTOCOL_START_BYTE, MESSAGE_LENGTH_STANDARD, COMMAND_TYPE_BUTTON, 0x48, 0x01
    };
    
    
    // ==================== UTILITY METHODS ====================
    
    /**
     * Calculate checksum for command data
     * Simple additive checksum of all bytes
     * 
     * @param data Command data bytes
     * @return Checksum byte
     */
    public static byte calculateChecksum(byte[] data) {
        int sum = 0;
        for (byte b : data) {
            sum += (b & 0xFF);
        }
        return (byte) (sum & 0xFF);
    }
    
    /**
     * Build a command packet with automatic checksum
     * 
     * @param commandType Type of command (usually COMMAND_TYPE_BUTTON)
     * @param commandData Command-specific data byte
     * @return Complete command packet
     */
    public static byte[] buildCommand(byte commandType, byte commandData) {
        byte[] cmd = new byte[5];
        cmd[0] = PROTOCOL_START_BYTE;
        cmd[1] = 0x02; // Length
        cmd[2] = commandType;
        cmd[3] = commandData;
        
        // Calculate checksum (sum of first 4 bytes)
        int checksum = (cmd[0] & 0xFF) + (cmd[1] & 0xFF) + (cmd[2] & 0xFF) + (cmd[3] & 0xFF);
        cmd[4] = (byte) (checksum & 0xFF);
        
        return cmd;
    }
    
    /**
     * Convert byte array to hex string for debugging
     * 
     * @param bytes Byte array to convert
     * @return Hex string representation
     */
    public static String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) {
            sb.append(String.format("%02X", b & 0xFF));
        }
        return sb.toString();
    }
    
    /**
     * Verify command checksum
     * 
     * @param command Command packet to verify
     * @return true if checksum is valid
     */
    public static boolean verifyChecksum(byte[] command) {
        if (command == null || command.length < 5) {
            return false;
        }
        
        int expectedChecksum = 0;
        for (int i = 0; i < command.length - 1; i++) {
            expectedChecksum += (command[i] & 0xFF);
        }
        
        return (byte)(expectedChecksum & 0xFF) == command[command.length - 1];
    }
}
