using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsCallback : AndroidJavaProxy
{
    public string msg;
    public AdsCallback() : base("com.unity.ble.Bluetooth$AdsCallback")
    {

    }

    public void onStartSuccess()
    {
        msg = "Started Advertising, called from unity";
        UnityMainThread.wkr.AddJob(() =>
        {
            BluetoothServer.Instance.connectingStatus.text = "Sending signal to computer";
        });
    }

    public void onStartFailure()
    {
        UnityMainThread.wkr.AddJob(() =>
        {
            BluetoothServer.Instance.connectingStatus.text = "Unable to send signal to computer";
        });
    }
}
