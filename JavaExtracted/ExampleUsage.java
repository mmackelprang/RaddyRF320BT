package com.myhomesmartlife.bluetooth.CleanedUp;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanResult;
import android.content.Context;
import android.util.Log;

/**
 * Example Usage of Radio Bluetooth Classes
 * 
 * This class demonstrates how to use the cleaned-up radio Bluetooth protocol
 * classes to connect to and control a radio device.
 * 
 * Basic flow:
 * 1. Initialize Bluetooth manager
 * 2. Scan for devices
 * 3. Connect to radio
 * 4. Send commands
 * 5. Receive and parse responses
 */
public class ExampleUsage {
    
    private static final String TAG = "RadioExample";
    
    private Context context;
    private RadioBluetoothManager bluetoothManager;
    private RadioCommandSender commandSender;
    private RadioProtocolHandler protocolHandler;
    
    /**
     * Initialize the radio communication system
     * 
     * @param context Application context
     */
    public void initialize(Context context) {
        this.context = context;
        
        // 1. Create Bluetooth manager
        bluetoothManager = new RadioBluetoothManager(context);
        
        // 2. Create command sender
        commandSender = new RadioCommandSender(bluetoothManager, context);
        
        // 3. Create protocol handler
        protocolHandler = new RadioProtocolHandler();
        
        // 4. Set up listeners
        setupListeners();
        
        Log.d(TAG, "Radio system initialized");
    }
    
    /**
     * Set up event listeners
     */
    private void setupListeners() {
        // Connection state listener
        bluetoothManager.setConnectionListener(new RadioBluetoothManager.ConnectionListener() {
            @Override
            public void onConnectionStateChanged(RadioBluetoothManager.ConnectionState newState) {
                Log.d(TAG, "Connection state: " + newState);
                
                if (newState == RadioBluetoothManager.ConnectionState.READY) {
                    Log.d(TAG, "Radio is ready for commands!");
                    // Can start sending commands now
                }
            }
            
            @Override
            public void onConnectionError(String error) {
                Log.e(TAG, "Connection error: " + error);
            }
        });
        
        // Data received listener
        bluetoothManager.setDataReceivedListener(new RadioBluetoothManager.DataReceivedListener() {
            @Override
            public void onDataReceived(byte[] data) {
                Log.d(TAG, "Data received: " + RadioProtocolCommands.bytesToHex(data));
                
                // Parse the received data
                protocolHandler.parseReceivedData(data);
            }
        });
        
        // Protocol data listener
        protocolHandler.setDataListener(new RadioProtocolHandler.RadioDataListener() {
            @Override
            public void onFrequencyChanged(String frequency, String band) {
                Log.d(TAG, "Frequency changed: " + frequency + " Hz, Band: " + band);
                // Update UI with new frequency
            }
            
            @Override
            public void onVolumeChanged(int volume) {
                Log.d(TAG, "Volume changed: " + volume);
                // Update UI with new volume
            }
            
            @Override
            public void onSignalStrengthChanged(int strength) {
                Log.d(TAG, "Signal strength: " + strength);
                // Update signal meter
            }
            
            @Override
            public void onStatusUpdate(RadioProtocolHandler.RadioStatus status) {
                Log.d(TAG, "Status update: " + status);
                // Update UI with full status
            }
        });
    }
    
    /**
     * Scan for nearby Bluetooth devices
     */
    public void scanForDevices() {
        BluetoothAdapter adapter = bluetoothManager.getBluetoothAdapter();
        if (adapter == null || !adapter.isEnabled()) {
            Log.e(TAG, "Bluetooth not available or not enabled");
            return;
        }
        
        BluetoothLeScanner scanner = adapter.getBluetoothLeScanner();
        if (scanner == null) {
            Log.e(TAG, "BLE scanner not available");
            return;
        }
        
        // Start scanning
        scanner.startScan(new ScanCallback() {
            @Override
            public void onScanResult(int callbackType, ScanResult result) {
                BluetoothDevice device = result.getDevice();
                String deviceName = device.getName();
                
                Log.d(TAG, "Found device: " + deviceName + " (" + device.getAddress() + ")");
                
                // Check if this is our radio device
                if (deviceName != null && deviceName.contains("RADIO")) {
                    // Stop scanning
                    BluetoothLeScanner scanner = bluetoothManager.getBluetoothAdapter().getBluetoothLeScanner();
                    if (scanner != null) {
                        scanner.stopScan(this);
                    }
                    
                    // Connect to the device
                    connectToRadio(device);
                }
            }
        });
        
        Log.d(TAG, "Scanning for devices...");
    }
    
    /**
     * Connect to a specific radio device
     * 
     * @param device The Bluetooth device to connect to
     */
    public void connectToRadio(BluetoothDevice device) {
        Log.d(TAG, "Connecting to: " + device.getName());
        bluetoothManager.connect(device);
    }
    
    /**
     * Disconnect from the radio
     */
    public void disconnect() {
        bluetoothManager.disconnect();
        Log.d(TAG, "Disconnected from radio");
    }
    
    
    // ==================== EXAMPLE COMMANDS ====================
    
    /**
     * Example: Set frequency to 145.500 MHz
     */
    public void exampleSetFrequency() {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        Log.d(TAG, "Setting frequency to 145.500 MHz");
        
        // Enter frequency mode
        commandSender.enterFrequencyMode();
        
        // Small delay between commands
        try {
            Thread.sleep(100);
            
            // Type: 1 4 5 . 5 0 0
            commandSender.pressNumber(1);
            Thread.sleep(100);
            commandSender.pressNumber(4);
            Thread.sleep(100);
            commandSender.pressNumber(5);
            Thread.sleep(100);
            commandSender.pressPoint();
            Thread.sleep(100);
            commandSender.pressNumber(5);
            Thread.sleep(100);
            commandSender.pressNumber(0);
            Thread.sleep(100);
            commandSender.pressNumber(0);
            
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }
    
    /**
     * Example: Adjust volume
     */
    public void exampleAdjustVolume(boolean increase) {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        if (increase) {
            Log.d(TAG, "Increasing volume");
            commandSender.volumeUp();
        } else {
            Log.d(TAG, "Decreasing volume");
            commandSender.volumeDown();
        }
    }
    
    /**
     * Example: Scan for channels
     */
    public void exampleScanChannels(boolean up) {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        if (up) {
            Log.d(TAG, "Scanning up");
            commandSender.frequencyUpFast();
        } else {
            Log.d(TAG, "Scanning down");
            commandSender.frequencyDownFast();
        }
    }
    
    /**
     * Example: Save current frequency to memory
     */
    public void exampleSaveToMemory() {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        Log.d(TAG, "Saving to memory");
        commandSender.saveMemo();
    }
    
    /**
     * Example: Switch band
     */
    public void exampleSwitchBand() {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        Log.d(TAG, "Switching band");
        commandSender.nextBand();
    }
    
    /**
     * Example: Change radio settings
     */
    public void exampleChangeSettings() {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        try {
            // Change demodulation mode
            Log.d(TAG, "Changing demodulation");
            commandSender.changeDemodulation();
            Thread.sleep(200);
            
            // Adjust squelch
            Log.d(TAG, "Adjusting squelch");
            commandSender.adjustSquelch();
            Thread.sleep(200);
            
            // Toggle stereo
            Log.d(TAG, "Toggling stereo");
            commandSender.toggleStereo();
            
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }
    
    /**
     * Example: Emergency SOS
     */
    public void exampleActivateSOS() {
        if (!commandSender.isReady()) {
            Log.w(TAG, "Not ready to send commands");
            return;
        }
        
        Log.d(TAG, "Activating SOS!");
        commandSender.activateSOS();
    }
    
    
    // ==================== CLEANUP ====================
    
    /**
     * Clean up resources when done
     */
    public void cleanup() {
        if (bluetoothManager != null) {
            bluetoothManager.close();
        }
        Log.d(TAG, "Cleanup complete");
    }
    
    
    // ==================== COMPLETE EXAMPLE ====================
    
    /**
     * Complete example showing full workflow
     */
    public static void fullExample(Context context) {
        Log.d(TAG, "=== Starting Full Radio Control Example ===");
        
        // Create and initialize
        ExampleUsage example = new ExampleUsage();
        example.initialize(context);
        
        // Start scanning for devices
        example.scanForDevices();
        
        // After connection is established (in the callback), you can:
        // - Set frequency
        // - Adjust volume
        // - Change settings
        // - Save to memory
        // etc.
        
        // When done, cleanup
        // example.cleanup();
        
        Log.d(TAG, "=== Example Complete ===");
    }
}
