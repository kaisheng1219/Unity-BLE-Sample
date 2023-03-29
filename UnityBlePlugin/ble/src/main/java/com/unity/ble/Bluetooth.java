package com.unity.ble;


import android.annotation.SuppressLint;
import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattServer;
import android.bluetooth.BluetoothGattServerCallback;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.bluetooth.le.AdvertiseCallback;
import android.bluetooth.le.AdvertiseData;
import android.bluetooth.le.AdvertiseSettings;
import android.bluetooth.le.BluetoothLeAdvertiser;
import android.content.Intent;
import android.os.ParcelUuid;
import android.content.Context;
import android.util.Log;

import java.nio.charset.StandardCharsets;
import java.util.UUID;

public class Bluetooth {
    public interface BluetoothServerCallback {
        public void onBluetoothStateChange(String state);
        public void onConnectionStateChange(String state);
        public void onCharacteristicReadRequest();
        public void onCharacteristicWriteRequest(String value);
    }

    public interface AdsCallback {
        public void onStartSuccess();
        public void onStartFailure();
    }

    private static final String serviceUUID = "c01cdc27-9eae-4a7c-bdca-e2bbdbd8b13e";
    private static final String controlCharUUID = "af2b5bf2-4ce8-436e-9689-6dbb74754fb8";
    private static final String stateCharUUID = "d3662fa3-5fb1-4195-ac0e-810d393abd34";
    private static final String cccdUUID = "00002902-0000-1000-8000-00805f9b34fb";

    private static final Bluetooth instance = new Bluetooth();
    private BluetoothServerCallback serverCallback;
    private AdsCallback adsCallback;
    private Activity mainActivity;
    private Context context;

    private BluetoothManager bluetoothManager;
    private BluetoothAdapter bleAdapter;
    private BluetoothLeAdvertiser bleAdvertiser;
    private BluetoothGattServer bluetoothGattServer;
    private BluetoothGattService bluetoothGattService;
    private BluetoothDevice connectedDevice;
    private BluetoothGattCharacteristic controlCharacteristic, stateCharacteristic;

    private AdvertiseSettings settings;
    private AdvertiseData data;

    private String debugMsg;
    private boolean status;

    public void setActivityAndContext(Context context) {
        mainActivity = (Activity) context;
        this.context = context;
    }

    public void setCallbacks(BluetoothServerCallback serverCallback, AdsCallback adsCallback) {
        BluetoothOnRequest.serverCallback = serverCallback;
        this.serverCallback = serverCallback;
        this.adsCallback = adsCallback;
    }

    public static Bluetooth getInstance() { return instance; }

    @SuppressLint("MissingPermission")
    public Bluetooth() { }

    public void initBluetooth() {
        bluetoothManager = (BluetoothManager) context.getSystemService(Context.BLUETOOTH_SERVICE);
        bleAdapter = bluetoothManager.getAdapter();
    }

    public boolean getIsBleSupported() {
        return bleAdapter.isMultipleAdvertisementSupported();
    }

    public boolean getBluetoothState() {
        return bleAdapter.isEnabled();
    }

    @SuppressLint("MissingPermission")
    public void requestToOnBluetooth() {
        mainActivity.runOnUiThread(() -> {
            try {
                Intent shareIntent = new Intent();
                shareIntent.setAction(Intent.ACTION_SEND);
                shareIntent.setClass(mainActivity, BluetoothOnRequest.class);
                mainActivity.startActivity(shareIntent);
            } catch (Exception e) {
                debugMsg = e.getMessage();
            }
        });
    }

    @SuppressLint("MissingPermission")
    public void createGattServer() {
        bluetoothGattServer = bluetoothManager.openGattServer(context, bluetoothGattServerCallback);
        createGattService();
    }

    @SuppressLint("MissingPermission")
    public void startAdvertise() {
        debugMsg = "Creating Ble Advertiser";
        bleAdvertiser = bleAdapter.getBluetoothLeAdvertiser();
        settings = new AdvertiseSettings.Builder()
                .setAdvertiseMode(AdvertiseSettings.ADVERTISE_MODE_LOW_LATENCY)
                .setTxPowerLevel(AdvertiseSettings.ADVERTISE_TX_POWER_LOW)
                .setConnectable(true)
                .setTimeout(0)
                .build();
        ParcelUuid pUuid = new ParcelUuid( UUID.randomUUID() );
        AdvertiseData data = new AdvertiseData.Builder()
                .setIncludeDeviceName( false )
                .addManufacturerData(1, "ctrler".getBytes(StandardCharsets.UTF_8))
                .build();
        debugMsg = "BLE Advertising settings done";
        bleAdvertiser.startAdvertising( settings, data, advertisingCallback );
    }

    @SuppressLint("MissingPermission")
    public void stopAdvertise() {
        bleAdvertiser.stopAdvertising(advertisingCallback);
    }

    public String getDebugMsg() { return debugMsg; }

    @SuppressLint("MissingPermission")
    public void setCharacteristicValue(String uuid, String value) {
        if (uuid.equals(controlCharUUID)) {
            controlCharacteristic.setValue(value);
            bluetoothGattServer.notifyCharacteristicChanged(connectedDevice, controlCharacteristic, false);
        } else if (uuid.equals(stateCharUUID)) {
            stateCharacteristic.setValue(value);
            bluetoothGattServer.notifyCharacteristicChanged(connectedDevice, stateCharacteristic, false);
        }
    }

    @SuppressLint("MissingPermission")
    private void createGattService() {
        UUID uuid = UUID.fromString(serviceUUID);
        bluetoothGattService = new BluetoothGattService(uuid, BluetoothGattService.SERVICE_TYPE_PRIMARY);
        controlCharacteristic = createIndicateCharacteristic(controlCharUUID);
        stateCharacteristic = createIndicateCharacteristic(stateCharUUID);
        bluetoothGattService.addCharacteristic(controlCharacteristic);
        bluetoothGattService.addCharacteristic(stateCharacteristic);
        bluetoothGattServer.addService(bluetoothGattService);
        debugMsg = "Created Gatt Service";
    }

    private BluetoothGattCharacteristic createIndicateCharacteristic(String uuid) {
        BluetoothGattDescriptor descriptor = new BluetoothGattDescriptor(UUID.fromString(cccdUUID),
                BluetoothGattDescriptor.PERMISSION_READ | BluetoothGattDescriptor.PERMISSION_WRITE);
        descriptor.setValue(BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE);

        BluetoothGattCharacteristic characteristic = new BluetoothGattCharacteristic(UUID.fromString(uuid),
                BluetoothGattCharacteristic.PROPERTY_READ | BluetoothGattCharacteristic.PROPERTY_WRITE | BluetoothGattCharacteristic.PROPERTY_NOTIFY,
                BluetoothGattCharacteristic.PERMISSION_READ | BluetoothGattCharacteristic.PERMISSION_WRITE);
        characteristic.addDescriptor(descriptor);

        return characteristic;
    }

    // GATT server callback for handling events such as connections and characteristic changes
    private BluetoothGattServerCallback bluetoothGattServerCallback =
            new BluetoothGattServerCallback() {
                @SuppressLint("MissingPermission")
                @Override
                public void onConnectionStateChange(BluetoothDevice device, int status, int newState) {
                    if (newState == BluetoothProfile.STATE_CONNECTED) {
                        super.onConnectionStateChange(device, status, newState);
                        connectedDevice = device;
                        bleAdvertiser.stopAdvertising(advertisingCallback);
                        serverCallback.onConnectionStateChange("Connected");
                    } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                        connectedDevice = null;
                        serverCallback.onConnectionStateChange("Disconnected");
                    }
                }

                @SuppressLint("MissingPermission")
                @Override
                public void onCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset,
                                                        BluetoothGattCharacteristic characteristic) {
                    if (connectedDevice != null && connectedDevice.equals(device)) {
                        bluetoothGattServer.sendResponse(device, requestId, BluetoothGatt.GATT_SUCCESS, offset,
                                characteristic.getValue());
                        serverCallback.onCharacteristicReadRequest();
                    } else {
                        bluetoothGattServer.sendResponse(device, requestId, BluetoothGatt.GATT_FAILURE, offset, null);
                    }
                }

                @SuppressLint("MissingPermission")
                @Override
                public void onCharacteristicWriteRequest(BluetoothDevice device, int requestId,
                                                         BluetoothGattCharacteristic characteristic,
                                                         boolean preparedWrite, boolean responseNeeded, int offset, byte[] value) {
                    if (connectedDevice != null && connectedDevice.equals(device)) {
                        characteristic.setValue(value);
                        if (responseNeeded)
                            bluetoothGattServer.sendResponse(device, requestId, BluetoothGatt.GATT_SUCCESS, offset, null);
                        if (characteristic.getUuid().toString().equals(stateCharUUID))
                            serverCallback.onCharacteristicWriteRequest(new String(value, StandardCharsets.UTF_8));
                    } else {
                        bluetoothGattServer.sendResponse(device, requestId, BluetoothGatt.GATT_FAILURE, offset, null);
                    }
                }

                @SuppressLint("MissingPermission")
                @Override
                public void onDescriptorWriteRequest(BluetoothDevice device, int requestId, BluetoothGattDescriptor descriptor, boolean preparedWrite, boolean responseNeeded, int offset, byte[] value) {
                    super.onDescriptorWriteRequest(device, requestId, descriptor, preparedWrite, responseNeeded, offset, value);
                    descriptor.setValue(value);
                    if (responseNeeded)
                        bluetoothGattServer.sendResponse(device, requestId, BluetoothGatt.GATT_SUCCESS, offset, value);
                }

                @SuppressLint("MissingPermission")
                @Override
                public void onDescriptorReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattDescriptor descriptor) {
                    super.onDescriptorReadRequest(device, requestId, offset, descriptor);
                }
            };

    private AdvertiseCallback advertisingCallback = new AdvertiseCallback() {
        @Override
        public void onStartSuccess(AdvertiseSettings settingsInEffect) {
            debugMsg = "BLE Advertising onStartSuccess";
            adsCallback.onStartSuccess();
            super.onStartSuccess(settingsInEffect);
        }
        @Override
        public void onStartFailure(int errorCode) {
            debugMsg = "BLE Advertising onStartFailure: " + errorCode;
            adsCallback.onStartFailure();
            super.onStartFailure(errorCode);
        }
    };
}
