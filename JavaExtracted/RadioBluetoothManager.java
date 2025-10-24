package com.myhomesmartlife.bluetooth.CleanedUp;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.content.Context;
import android.util.Log;

import java.util.UUID;

/**
 * Radio Bluetooth Connection Manager
 * 
 * Manages BLE (Bluetooth Low Energy) connection to the radio device.
 * Handles GATT service discovery, characteristic setup, and connection state.
 * 
 * Service UUIDs:
 * - Main Service: Standard BLE service containing radio characteristics
 * - Write Characteristic: 0000ff13-0000-1000-8000-00805f9b34fb (for sending commands)
 * - Notify Characteristic: 0000ff14-0000-1000-8000-00805f9b34fb (for receiving data)
 */
public class RadioBluetoothManager {
    
    private static final String TAG = "RadioBluetooth";
    
    // ==================== GATT UUIDs ====================
    
    /** UUID for the write characteristic (send commands to radio) */
    public static final UUID CHARACTERISTIC_WRITE_UUID = 
        UUID.fromString("0000ff13-0000-1000-8000-00805f9b34fb");
    
    /** UUID for the notify characteristic (receive data from radio) */
    public static final UUID CHARACTERISTIC_NOTIFY_UUID = 
        UUID.fromString("0000ff14-0000-1000-8000-00805f9b34fb");
    
    /** Standard Client Characteristic Configuration Descriptor UUID */
    public static final UUID CLIENT_CHARACTERISTIC_CONFIG = 
        UUID.fromString("00002902-0000-1000-8000-00805f9b34fb");
    
    
    // ==================== CONNECTION STATE ====================
    
    public enum ConnectionState {
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        DISCOVERING_SERVICES,
        READY
    }
    
    
    // ==================== MEMBER VARIABLES ====================
    
    private final Context context;
    private final BluetoothManager bluetoothManager;
    private final BluetoothAdapter bluetoothAdapter;
    
    private BluetoothGatt bluetoothGatt;
    private BluetoothGattService gattService;
    private BluetoothGattCharacteristic writeCharacteristic;
    private BluetoothGattCharacteristic notifyCharacteristic;
    
    private ConnectionState connectionState = ConnectionState.DISCONNECTED;
    private ConnectionListener connectionListener;
    private DataReceivedListener dataReceivedListener;
    
    
    // ==================== LISTENER INTERFACES ====================
    
    /**
     * Listener for connection state changes
     */
    public interface ConnectionListener {
        void onConnectionStateChanged(ConnectionState newState);
        void onConnectionError(String error);
    }
    
    /**
     * Listener for received data from radio
     */
    public interface DataReceivedListener {
        void onDataReceived(byte[] data);
    }
    
    
    // ==================== CONSTRUCTOR ====================
    
    /**
     * Create a new RadioBluetoothManager
     * 
     * @param context Application context
     */
    public RadioBluetoothManager(Context context) {
        this.context = context;
        this.bluetoothManager = (BluetoothManager) context.getSystemService(Context.BLUETOOTH_SERVICE);
        this.bluetoothAdapter = bluetoothManager.getAdapter();
    }
    
    
    // ==================== CONNECTION MANAGEMENT ====================
    
    /**
     * Connect to a radio device
     * 
     * @param device The Bluetooth device to connect to
     * @return true if connection attempt started
     */
    public boolean connect(BluetoothDevice device) {
        if (bluetoothAdapter == null || device == null) {
            Log.e(TAG, "BluetoothAdapter not initialized or device null");
            return false;
        }
        
        if (bluetoothGatt != null) {
            Log.w(TAG, "Closing existing GATT connection");
            bluetoothGatt.close();
            bluetoothGatt = null;
        }
        
        Log.d(TAG, "Connecting to device: " + device.getAddress());
        connectionState = ConnectionState.CONNECTING;
        notifyConnectionStateChanged();
        
        // Connect to GATT server on the device
        bluetoothGatt = device.connectGatt(context, false, gattCallback);
        return bluetoothGatt != null;
    }
    
    /**
     * Disconnect from the radio device
     */
    public void disconnect() {
        if (bluetoothGatt != null) {
            Log.d(TAG, "Disconnecting from device");
            bluetoothGatt.disconnect();
        }
    }
    
    /**
     * Close and cleanup the GATT connection
     */
    public void close() {
        if (bluetoothGatt != null) {
            bluetoothGatt.close();
            bluetoothGatt = null;
        }
        connectionState = ConnectionState.DISCONNECTED;
        writeCharacteristic = null;
        notifyCharacteristic = null;
        gattService = null;
    }
    
    
    // ==================== DATA TRANSMISSION ====================
    
    /**
     * Send a command to the radio device
     * 
     * @param command Command byte array to send
     * @return true if write was initiated successfully
     */
    public boolean sendCommand(byte[] command) {
        if (connectionState != ConnectionState.READY) {
            Log.e(TAG, "Not ready to send commands. State: " + connectionState);
            return false;
        }
        
        if (writeCharacteristic == null) {
            Log.e(TAG, "Write characteristic not available");
            return false;
        }
        
        writeCharacteristic.setValue(command);
        boolean result = bluetoothGatt.writeCharacteristic(writeCharacteristic);
        
        Log.d(TAG, "Sending command: " + RadioProtocolCommands.bytesToHex(command) + 
                   " Result: " + result);
        
        return result;
    }
    
    /**
     * Enable notifications from the radio device
     * 
     * @return true if notification setup was initiated
     */
    private boolean enableNotifications() {
        if (bluetoothGatt == null || notifyCharacteristic == null) {
            Log.e(TAG, "GATT or notify characteristic not available");
            return false;
        }
        
        // Enable local notifications
        boolean success = bluetoothGatt.setCharacteristicNotification(notifyCharacteristic, true);
        
        if (!success) {
            Log.e(TAG, "Failed to enable characteristic notification");
            return false;
        }
        
        // Enable remote notifications by writing to the descriptor
        BluetoothGattDescriptor descriptor = notifyCharacteristic.getDescriptor(CLIENT_CHARACTERISTIC_CONFIG);
        if (descriptor == null) {
            Log.e(TAG, "Notification descriptor not found");
            return false;
        }
        
        descriptor.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);
        success = bluetoothGatt.writeDescriptor(descriptor);
        
        Log.d(TAG, "Notification descriptor write initiated: " + success);
        return success;
    }
    
    
    // ==================== GATT CALLBACK ====================
    
    /**
     * GATT callback for handling connection events and data
     */
    private final BluetoothGattCallback gattCallback = new BluetoothGattCallback() {
        
        @Override
        public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
            if (newState == BluetoothProfile.STATE_CONNECTED) {
                Log.d(TAG, "Connected to GATT server");
                connectionState = ConnectionState.CONNECTED;
                notifyConnectionStateChanged();
                
                // Discover services
                Log.d(TAG, "Attempting to start service discovery");
                connectionState = ConnectionState.DISCOVERING_SERVICES;
                gatt.discoverServices();
                
            } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                Log.d(TAG, "Disconnected from GATT server");
                connectionState = ConnectionState.DISCONNECTED;
                notifyConnectionStateChanged();
                
                if (bluetoothGatt != null) {
                    bluetoothGatt.close();
                    bluetoothGatt = null;
                }
            }
        }
        
        @Override
        public void onServicesDiscovered(BluetoothGatt gatt, int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.d(TAG, "Services discovered successfully");
                
                // Find our service and characteristics
                boolean setupSuccess = setupCharacteristics(gatt);
                
                if (setupSuccess) {
                    // Send handshake command
                    sendCommand(RadioProtocolCommands.CMD_HANDSHAKE);
                    
                    // Enable notifications
                    enableNotifications();
                    
                    connectionState = ConnectionState.READY;
                    notifyConnectionStateChanged();
                    Log.d(TAG, "Radio is ready for communication");
                } else {
                    Log.e(TAG, "Failed to setup characteristics");
                    if (connectionListener != null) {
                        connectionListener.onConnectionError("Failed to find required characteristics");
                    }
                }
            } else {
                Log.e(TAG, "Service discovery failed with status: " + status);
                if (connectionListener != null) {
                    connectionListener.onConnectionError("Service discovery failed");
                }
            }
        }
        
        @Override
        public void onCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
            // Data received from radio
            byte[] data = characteristic.getValue();
            Log.d(TAG, "Data received: " + RadioProtocolCommands.bytesToHex(data));
            
            if (dataReceivedListener != null) {
                dataReceivedListener.onDataReceived(data);
            }
        }
        
        @Override
        public void onCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, int status) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.d(TAG, "Characteristic write successful");
            } else {
                Log.e(TAG, "Characteristic write failed with status: " + status);
            }
        }
    };
    
    
    // ==================== HELPER METHODS ====================
    
    /**
     * Setup the GATT characteristics for communication
     * 
     * @param gatt The GATT instance
     * @return true if setup successful
     */
    private boolean setupCharacteristics(BluetoothGatt gatt) {
        // Iterate through all services to find our characteristics
        for (BluetoothGattService service : gatt.getServices()) {
            Log.d(TAG, "Service UUID: " + service.getUuid());
            
            for (BluetoothGattCharacteristic characteristic : service.getCharacteristics()) {
                UUID uuid = characteristic.getUuid();
                Log.d(TAG, "  Characteristic UUID: " + uuid);
                
                if (CHARACTERISTIC_WRITE_UUID.equals(uuid)) {
                    writeCharacteristic = characteristic;
                    gattService = service;
                    Log.d(TAG, "Found write characteristic");
                }
                
                if (CHARACTERISTIC_NOTIFY_UUID.equals(uuid)) {
                    notifyCharacteristic = characteristic;
                    Log.d(TAG, "Found notify characteristic");
                }
            }
        }
        
        return writeCharacteristic != null && notifyCharacteristic != null;
    }
    
    /**
     * Notify connection state changed
     */
    private void notifyConnectionStateChanged() {
        if (connectionListener != null) {
            connectionListener.onConnectionStateChanged(connectionState);
        }
    }
    
    
    // ==================== GETTERS & SETTERS ====================
    
    public ConnectionState getConnectionState() {
        return connectionState;
    }
    
    public boolean isReady() {
        return connectionState == ConnectionState.READY;
    }
    
    public void setConnectionListener(ConnectionListener listener) {
        this.connectionListener = listener;
    }
    
    public void setDataReceivedListener(DataReceivedListener listener) {
        this.dataReceivedListener = listener;
    }
    
    public BluetoothAdapter getBluetoothAdapter() {
        return bluetoothAdapter;
    }
}
