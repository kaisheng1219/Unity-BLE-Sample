package com.unity.ble;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

public class BluetoothOnRequest extends Activity {
    public static Bluetooth.BluetoothServerCallback serverCallback;

    private void myFinish(String state) {
        if (serverCallback != null) {
            serverCallback.onBluetoothStateChange(state);
        }

        serverCallback = null;
        finish();
    }

    @SuppressLint("MissingPermission")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Intent intent = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
        startActivityForResult(intent, 1);
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == 1) {
            if (resultCode == RESULT_OK) {
                // Bluetooth is now turned on
                myFinish("On");
            } else {
                // User declined to turn on Bluetooth
                myFinish("Off");
            }
        }
    }
}
