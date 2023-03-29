using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class BluetoothServerCallback : AndroidJavaProxy
{
    public string msg;

    public BluetoothServerCallback() : base("com.unity.ble.Bluetooth$BluetoothServerCallback")
    {
        msg = "Created Call Back";
    }

    public void onBluetoothStateChange(string state)
    {
        if (state == "On")
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                try
                {
                    if (BluetoothServer.PluginInstance.Call<bool>("getIsBleSupported"))
                    {
                        BluetoothServer.PluginInstance.Call("createGattServer");
                        BluetoothServer.Instance.status.text = "Press connect to start connecting to computer";
                        BluetoothServer.Instance.connectButton.interactable = true;
                    }
                    else
                    {
                        BluetoothServer.Instance.status.text = "Device does not support Bluetooth LE\nplease use the server method for connection";
                        BluetoothServer.Instance.connectButton.interactable = false;
                    }
                } catch (Exception e)
                {
                    BluetoothServer.Instance.status.text = e.StackTrace;
                }
                
            });
            msg = "Bluetooth turned on";
        }
        else
        {
            UnityMainThread.wkr.AddJob(() =>
            {
                BluetoothServer.Instance.status.text = "Please turn on Bluetooth to allow connection to computer";
                BluetoothServer.Instance.SetConnectBtn(false);
            });
            msg = "Bluetooth turned off";
        }
    }

    public void onConnectionStateChange(string state)
    {
        
    }

    public void onCharacteristicReadRequest()
    {

    }
    public void onCharacteristicWriteRequest(string value)
    {
        if (value == "PC_CONNECTED")
            UnityMainThread.wkr.AddJob(() =>
            {
                GameStateManager.Instance.GameState = GameStateManager.State.Bluetooth;
                SceneManager.LoadScene(3);
            });
    }
}
