package com.myhomesmartlife.bluetooth.CleanedUp;

import android.content.Context;
import android.os.Vibrator;
import android.util.Log;

/**
 * Radio Command Sender
 * 
 * High-level interface for sending commands to the radio device.
 * Provides convenient methods for common radio operations.
 * 
 * Usage:
 * 1. Create RadioBluetoothManager and connect to device
 * 2. Create RadioCommandSender with the manager
 * 3. Call methods to control the radio (e.g., setFrequency(), adjustVolume())
 */
public class RadioCommandSender {
    
    private static final String TAG = "RadioCommandSender";
    
    private final RadioBluetoothManager bluetoothManager;
    private final Vibrator vibrator;
    private boolean hapticFeedbackEnabled = true;
    
    /**
     * Create a new RadioCommandSender
     * 
     * @param bluetoothManager The Bluetooth manager for sending commands
     * @param context Application context (for vibration feedback)
     */
    public RadioCommandSender(RadioBluetoothManager bluetoothManager, Context context) {
        this.bluetoothManager = bluetoothManager;
        this.vibrator = (Vibrator) context.getSystemService(Context.VIBRATOR_SERVICE);
    }
    
    
    // ==================== BASIC OPERATIONS ====================
    
    /**
     * Send a raw command to the radio
     * 
     * @param command Command byte array
     * @return true if sent successfully
     */
    public boolean sendCommand(byte[] command) {
        if (!bluetoothManager.isReady()) {
            Log.w(TAG, "Bluetooth not ready");
            return false;
        }
        
        // Provide haptic feedback
        if (hapticFeedbackEnabled && vibrator != null) {
            vibrator.vibrate(100); // 100ms vibration
        }
        
        return bluetoothManager.sendCommand(command);
    }
    
    /**
     * Send handshake command to establish communication
     * 
     * @return true if sent successfully
     */
    public boolean sendHandshake() {
        Log.d(TAG, "Sending handshake");
        return sendCommand(RadioProtocolCommands.CMD_HANDSHAKE);
    }
    
    
    // ==================== POWER & SYSTEM ====================
    
    /**
     * Toggle radio power
     * 
     * @return true if sent successfully
     */
    public boolean powerToggle() {
        return sendCommand(RadioProtocolCommands.CMD_POWER);
    }
    
    /**
     * Power off (long press)
     * 
     * @return true if sent successfully
     */
    public boolean powerOff() {
        return sendCommand(RadioProtocolCommands.CMD_POWER_LONG);
    }
    
    /**
     * Toggle Bluetooth mode
     * 
     * @return true if sent successfully
     */
    public boolean toggleBluetooth() {
        return sendCommand(RadioProtocolCommands.CMD_BLUETOOTH);
    }
    
    
    // ==================== BAND SELECTION ====================
    
    /**
     * Switch to next band
     * 
     * @return true if sent successfully
     */
    public boolean nextBand() {
        return sendCommand(RadioProtocolCommands.CMD_BAND);
    }
    
    /**
     * Switch to sub-band
     * 
     * @return true if sent successfully
     */
    public boolean selectSubBand() {
        return sendCommand(RadioProtocolCommands.CMD_SUB_BAND);
    }
    
    /**
     * Band long press
     * 
     * @return true if sent successfully
     */
    public boolean bandLongPress() {
        return sendCommand(RadioProtocolCommands.CMD_BAND_LONG_PRESS);
    }
    
    
    // ==================== FREQUENCY CONTROL ====================
    
    /**
     * Enter frequency mode
     * 
     * @return true if sent successfully
     */
    public boolean enterFrequencyMode() {
        return sendCommand(RadioProtocolCommands.CMD_FREQ);
    }
    
    /**
     * Frequency up (short press)
     * 
     * @return true if sent successfully
     */
    public boolean frequencyUp() {
        return sendCommand(RadioProtocolCommands.CMD_UP_SHORT);
    }
    
    /**
     * Frequency up (long press - fast scan)
     * 
     * @return true if sent successfully
     */
    public boolean frequencyUpFast() {
        return sendCommand(RadioProtocolCommands.CMD_UP_LONG);
    }
    
    /**
     * Frequency down (short press)
     * 
     * @return true if sent successfully
     */
    public boolean frequencyDown() {
        return sendCommand(RadioProtocolCommands.CMD_DOWN_SHORT);
    }
    
    /**
     * Frequency down (long press - fast scan)
     * 
     * @return true if sent successfully
     */
    public boolean frequencyDownFast() {
        return sendCommand(RadioProtocolCommands.CMD_DOWN_LONG);
    }
    
    
    // ==================== VOLUME CONTROL ====================
    
    /**
     * Increase volume
     * 
     * @return true if sent successfully
     */
    public boolean volumeUp() {
        return sendCommand(RadioProtocolCommands.CMD_VOLUME_UP);
    }
    
    /**
     * Decrease volume
     * 
     * @return true if sent successfully
     */
    public boolean volumeDown() {
        return sendCommand(RadioProtocolCommands.CMD_VOLUME_DOWN);
    }
    
    
    // ==================== NUMBER INPUT ====================
    
    /**
     * Press a number button (0-9)
     * 
     * @param number Number to press (0-9)
     * @return true if sent successfully
     */
    public boolean pressNumber(int number) {
        byte[] command;
        
        switch (number) {
            case 0: command = RadioProtocolCommands.CMD_NUMBER_0; break;
            case 1: command = RadioProtocolCommands.CMD_NUMBER_1; break;
            case 2: command = RadioProtocolCommands.CMD_NUMBER_2; break;
            case 3: command = RadioProtocolCommands.CMD_NUMBER_3; break;
            case 4: command = RadioProtocolCommands.CMD_NUMBER_4; break;
            case 5: command = RadioProtocolCommands.CMD_NUMBER_5; break;
            case 6: command = RadioProtocolCommands.CMD_NUMBER_6; break;
            case 7: command = RadioProtocolCommands.CMD_NUMBER_7; break;
            case 8: command = RadioProtocolCommands.CMD_NUMBER_8; break;
            case 9: command = RadioProtocolCommands.CMD_NUMBER_9; break;
            default:
                Log.w(TAG, "Invalid number: " + number);
                return false;
        }
        
        return sendCommand(command);
    }
    
    /**
     * Long press a number button (0-9)
     * 
     * @param number Number to long press (0-9)
     * @return true if sent successfully
     */
    public boolean pressNumberLong(int number) {
        byte[] command;
        
        switch (number) {
            case 0: command = RadioProtocolCommands.CMD_NUMBER_0_LONG; break;
            case 1: command = RadioProtocolCommands.CMD_NUMBER_1_LONG; break;
            case 2: command = RadioProtocolCommands.CMD_NUMBER_2_LONG; break;
            case 3: command = RadioProtocolCommands.CMD_NUMBER_3_LONG; break;
            case 4: command = RadioProtocolCommands.CMD_NUMBER_4_LONG; break;
            case 5: command = RadioProtocolCommands.CMD_NUMBER_5_LONG; break;
            case 6: command = RadioProtocolCommands.CMD_NUMBER_6_LONG; break;
            case 7: command = RadioProtocolCommands.CMD_NUMBER_7_LONG; break;
            case 8: command = RadioProtocolCommands.CMD_NUMBER_8_LONG; break;
            case 9: command = RadioProtocolCommands.CMD_NUMBER_9_LONG; break;
            default:
                Log.w(TAG, "Invalid number: " + number);
                return false;
        }
        
        return sendCommand(command);
    }
    
    /**
     * Press decimal point button
     * 
     * @return true if sent successfully
     */
    public boolean pressPoint() {
        return sendCommand(RadioProtocolCommands.CMD_POINT);
    }
    
    /**
     * Press back button
     * 
     * @return true if sent successfully
     */
    public boolean pressBack() {
        return sendCommand(RadioProtocolCommands.CMD_BACK);
    }
    
    
    // ==================== AUDIO MODES ====================
    
    /**
     * Toggle music mode
     * 
     * @return true if sent successfully
     */
    public boolean toggleMusic() {
        return sendCommand(RadioProtocolCommands.CMD_MUSIC);
    }
    
    /**
     * Play/Pause
     * 
     * @return true if sent successfully
     */
    public boolean playPause() {
        return sendCommand(RadioProtocolCommands.CMD_PLAY);
    }
    
    /**
     * Next track
     * 
     * @return true if sent successfully
     */
    public boolean nextTrack() {
        return sendCommand(RadioProtocolCommands.CMD_STEP);
    }
    
    /**
     * Toggle loop/circle mode
     * 
     * @return true if sent successfully
     */
    public boolean toggleCircle() {
        return sendCommand(RadioProtocolCommands.CMD_CIRCLE);
    }
    
    
    // ==================== RADIO SETTINGS ====================
    
    /**
     * Change demodulation mode
     * 
     * @return true if sent successfully
     */
    public boolean changeDemodulation() {
        return sendCommand(RadioProtocolCommands.CMD_DEMODULATION);
    }
    
    /**
     * Change bandwidth
     * 
     * @return true if sent successfully
     */
    public boolean changeBandwidth() {
        return sendCommand(RadioProtocolCommands.CMD_BANDWIDTH);
    }
    
    /**
     * Adjust squelch
     * 
     * @return true if sent successfully
     */
    public boolean adjustSquelch() {
        return sendCommand(RadioProtocolCommands.CMD_SQ);
    }
    
    /**
     * Toggle stereo mode
     * 
     * @return true if sent successfully
     */
    public boolean toggleStereo() {
        return sendCommand(RadioProtocolCommands.CMD_STEREO);
    }
    
    /**
     * Toggle DE (emphasis)
     * 
     * @return true if sent successfully
     */
    public boolean toggleDE() {
        return sendCommand(RadioProtocolCommands.CMD_DE);
    }
    
    
    // ==================== MEMORY & PRESETS ====================
    
    /**
     * Access preset/memory
     * 
     * @return true if sent successfully
     */
    public boolean accessPreset() {
        return sendCommand(RadioProtocolCommands.CMD_PRESET);
    }
    
    /**
     * Access memo
     * 
     * @return true if sent successfully
     */
    public boolean accessMemo() {
        return sendCommand(RadioProtocolCommands.CMD_MEMO);
    }
    
    /**
     * Long press memo (save)
     * 
     * @return true if sent successfully
     */
    public boolean saveMemo() {
        return sendCommand(RadioProtocolCommands.CMD_MEMO_LONG);
    }
    
    
    // ==================== RECORDING ====================
    
    /**
     * Toggle recording
     * 
     * @return true if sent successfully
     */
    public boolean toggleRecording() {
        return sendCommand(RadioProtocolCommands.CMD_REC);
    }
    
    
    // ==================== EMERGENCY ====================
    
    /**
     * Activate SOS
     * 
     * @return true if sent successfully
     */
    public boolean activateSOS() {
        return sendCommand(RadioProtocolCommands.CMD_SOS);
    }
    
    /**
     * SOS long press
     * 
     * @return true if sent successfully
     */
    public boolean sosLongPress() {
        return sendCommand(RadioProtocolCommands.CMD_SOS_LONG);
    }
    
    /**
     * Activate alarm
     * 
     * @return true if sent successfully
     */
    public boolean activateAlarm() {
        return sendCommand(RadioProtocolCommands.CMD_ALARM_CLICK);
    }
    
    
    // ==================== SETTINGS ====================
    
    /**
     * Enable or disable haptic feedback
     * 
     * @param enabled true to enable vibration on button press
     */
    public void setHapticFeedbackEnabled(boolean enabled) {
        this.hapticFeedbackEnabled = enabled;
    }
    
    /**
     * Check if haptic feedback is enabled
     * 
     * @return true if enabled
     */
    public boolean isHapticFeedbackEnabled() {
        return hapticFeedbackEnabled;
    }
    
    /**
     * Check if ready to send commands
     * 
     * @return true if Bluetooth is connected and ready
     */
    public boolean isReady() {
        return bluetoothManager.isReady();
    }
}
