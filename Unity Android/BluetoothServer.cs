using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Android;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BluetoothServer : MonoBehaviour
{
    [SerializeField] public TMP_Text status, connectingStatus;
    [SerializeField] public Button connectButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject connectingPanel;

    public static BluetoothServer Instance;
    const string pluginName = "com.unity.ble.Bluetooth";
    static AndroidJavaClass pluginClass;
    static AndroidJavaObject pluginInstance;
    AndroidJavaClass jc;
    AndroidJavaObject jo;

    BluetoothServerCallback bleCallback;
    AdsCallback adsCallback;

    float advertiseTime;
    bool adsStarted;

    public static AndroidJavaClass PluginClass
    {
        get
        {
            if (pluginClass == null)
                pluginClass = new AndroidJavaClass(pluginName);
            return pluginClass;
        }
    }

    public static AndroidJavaObject PluginInstance
    {
        get
        {
            if (pluginInstance == null)
                pluginInstance = PluginClass.CallStatic<AndroidJavaObject>("getInstance");
            return pluginInstance;
        }
    }

    void Awake()
    {
        if (Instance != null) { return; }
        Instance = this;
        DontDestroyOnLoad(Instance);
    }


    void Start()
    {
        Permission.RequestUserPermission("android.permission.BLUETOOTH");
        Permission.RequestUserPermission("android.permission.BLUETOOTH_ADMIN");
        Permission.RequestUserPermission("android.permission.BLUETOOTH_ADVERTISE");
        Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
        connectingPanel.SetActive(false);
        status.text = "Please turn on Bluetooth to allow connection to computer";

        try
        {
            bleCallback = new();
            adsCallback = new();
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            PluginInstance.Call("setActivityAndContext", jo);
            PluginInstance.Call("setCallbacks", bleCallback, adsCallback);
            InitBluetooth();
        }
        catch (Exception ex)
        {
            status.text = ex.StackTrace;
            //status.text = ex.Message;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (adsStarted)
        {
            advertiseTime -= Time.deltaTime;
            if (advertiseTime <= 0)
            {
                PluginInstance.Call("stopAdvertise");
                connectingPanel.SetActive(false);
                adsStarted = false;
            }
        }
    }

    void InitBluetooth()
    {
        try
        {
            bleCallback = new();
            adsCallback = new();
            AndroidJavaClass jc = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            PluginInstance.Call("setActivityAndContext", jo);
            PluginInstance.Call("setCallbacks", bleCallback, adsCallback);
            PluginInstance.Call("initBluetooth");

            if (PluginInstance.Call<bool>("getBluetoothState"))
            {
                if (PluginInstance.Call<bool>("getIsBleSupported"))
                {
                    PluginInstance.Call("createGattServer");
                    status.text = "Press connect to start connecting to computer";
                    connectButton.interactable = true;
                }
                else
                {
                    status.text = "Device does not support Bluetooth LE\nplease use the server method for connection";
                    connectButton.interactable = false;
                }
            }
            else
            {
                status.text = "Please turn on Bluetooth to allow connection to computer";
                PluginInstance.Call("requestToOnBluetooth");
            }
        }
        catch (Exception ex)
        {
            status.text = ex.Message;
        }
    }

    public void StartAdv()
    {
        adsStarted = true;
        advertiseTime = 10f;
        connectingPanel.SetActive(true);
        try
        {
            PluginInstance.Call("startAdvertise");
        } catch (Exception ex)
        {
            status.text = ex.Message;
        }
    }

    public void Back()
    {
        Instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene(0);
    }

    public void SetConnectBtn(bool interactable)
    {
        connectButton.interactable = interactable;
    }

    public void SetCharacteristicValue(string uuid, string message)
    {
        PluginInstance.Call("setCharacteristicValue", uuid, message);
    }
}
