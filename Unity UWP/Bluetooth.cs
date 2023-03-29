using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Vehicles.Car;

#if ENABLE_WINMD_SUPPORT
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
#endif

public class Bluetooth : MonoBehaviour
{
    private GameObject MainPanelTextObj, ConnectPanelTextObj, connectingPanel;
    private TMP_Text MainPanelText, ConnectingPanelText;

    private string mainPanelText, connectingPanelText;
    private bool isInitialized, isConnectingPanelActive, isConnected, switchedScene;

    private const string ServiceUUID = "c01cdc27-9eae-4a7c-bdca-e2bbdbd8b13e";
    private const string controlCharUUID = "af2b5bf2-4ce8-436e-9689-6dbb74754fb8";
    private const string stateCharUUID = "d3662fa3-5fb1-4195-ac0e-810d393abd34";

    [HideInInspector] public BluetoothStatus status;
    public enum BluetoothStatus
    {
        Searching, Connected, Disconnected, Unavailable, Available, LESupported, LENotSupported
    }

    public static Bluetooth Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else return;

        DontDestroyOnLoad(Instance);
    }

    async void Start()
    {
        if (isInitialized) return;

        MainPanelTextObj = GameObject.Find("Main Panel Text");
        ConnectPanelTextObj = GameObject.Find("Connecting Panel Text");
        connectingPanel = GameObject.Find("Connecting Panel");

        MainPanelText = MainPanelTextObj.GetComponent<TMP_Text>();
        ConnectingPanelText = ConnectPanelTextObj.GetComponent<TMP_Text>();

        mainPanelText = "";
        isConnectingPanelActive = true;
        connectingPanel.SetActive(true);
        connectingPanelText = "Initializing Bluetooth";
        ConnectingPanelText.text = connectingPanelText;
#if ENABLE_WINMD_SUPPORT
        isInitialized = await InitBluetooth();
#endif
        connectingPanel.SetActive(false);
    }

    #region Bluetooth Variables
#if ENABLE_WINMD_SUPPORT
        BluetoothLEAdvertisementWatcher watcher;
        IReadOnlyList<Radio> radios;
        Radio bleRadio;
        BluetoothLEDevice bleDevice;
        GattCharacteristic controlChar, stateChar;
#endif
    #endregion

#if ENABLE_WINMD_SUPPORT
    async Task<bool> InitBluetooth()
    {
        await CheckIfHasBluetoothHardwareAsync();
        if (status == BluetoothStatus.Available)
        {
            await CheckIfSupportBluetoothLEAsync();
            if (status == BluetoothStatus.LESupported)
            {
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;
                watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;
                watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);
        
                watcher.Received += OnAdvertisementReceived;
                watcher.Stopped += OnAdvertisementWatcherStopped;
                InitRadio();
                if (bleRadio.State == RadioState.Off)
                    mainPanelText = "Please turn on Bluetooth";
                else
                {
                    StartFindingController();
                    mainPanelText = "Press the connect button on the controller to start linking";
                }
            } 
            else 
            {
                mainPanelText = "Your device does not support Bluetooth Low Energy,\nplease use the server connect method";
            }
        } 
        else if (status == BluetoothStatus.Unavailable) 
        {
            mainPanelText = "Your device does not have Bluetooth capability,\nplease use the server connect method";
        }
        await Task.Delay(700);
        isInitialized = true;
        isConnectingPanelActive = false;
        return true;
    }

    async Task<bool> CheckIfHasBluetoothHardwareAsync()
    {
        radios = await Radio.GetRadiosAsync();
        var hasBluetooth = radios.Any(radio => radio.Kind == RadioKind.Bluetooth);
        if (hasBluetooth)
            status = BluetoothStatus.Available;
        else
            status = BluetoothStatus.Unavailable;
        
        return true;
    }

    async Task<bool> CheckIfSupportBluetoothLEAsync()
    {
        BluetoothAdapter adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter.IsLowEnergySupported)
            status = BluetoothStatus.LESupported;
        else
            status = BluetoothStatus.LENotSupported;
       
        return true;
    }

    void InitRadio() 
    {
        bleRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);  
        bleRadio.StateChanged += BluetoothRadioStateChanged;
    }

    void BluetoothRadioStateChanged(Radio sender, object args)
    {
        if (sender.State == RadioState.On)
        {
            mainPanelText = "Press the connect button on the controller to start linking";
            watcher.Start();
        } 
        else if (sender.State == RadioState.Off)
        {
            watcher.Stop();
            mainPanelText = "Please turn on Bluetooth";
        }
    }

    private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        if (bleDevice != null)
            return;
        var manufacturerSections = eventArgs.Advertisement.ManufacturerData;
        if (manufacturerSections.Count > 0)
        {
            var manufacturerData = manufacturerSections[0];
            var containedData = new byte[manufacturerData.Data.Length];
            using (var reader = DataReader.FromBuffer(manufacturerData.Data))
            {
                reader.ReadBytes(containedData);
            }
            
            if (Encoding.ASCII.GetString(containedData) == "ctrler") 
            {
                isConnectingPanelActive = true;
                connectingPanelText = "Controller found, connecting";
                bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
                watcher.Stop();
            }
        }
    }

    private async void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
    {
        if (bleDevice != null) {
            GattDeviceServicesResult result = await bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success) {
                var services = result.Services;
                foreach (var service in services) {
                    if (service.Uuid.ToString() == ServiceUUID) {
                        GattCharacteristicsResult serviceResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        if (serviceResult.Status == GattCommunicationStatus.Success) {
                            var characteristics = serviceResult.Characteristics;
                            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                            foreach (var ch in characteristics) {
                                if (ch.Uuid.ToString() == controlCharUUID) {
                                    controlChar = ch;
                                    await ch.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue);
                                    controlChar.ValueChanged += ControlCharacteristicValueChanged;
                                } else if (ch.Uuid.ToString() == stateCharUUID) {
                                    stateChar = ch;
                                    await ch.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(cccdValue);
                                    stateChar.ValueChanged += StateCharacteristicValueChanged;
                                }
                            }
                        }
                        byte[] msgs = Encoding.ASCII.GetBytes("PC_CONNECTED");
                        await stateChar.WriteValueAsync(msgs.AsBuffer());
                        connectingPanelText = "Connected to controller";
                        isConnected = true;
                        isConnectingPanelActive = false;
                        break;
                    }
                }
            }
        }
    }

    private void ControlCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var reader = DataReader.FromBuffer(args.CharacteristicValue);
        byte[] input = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(input);
        string[] controllerMsg = Encoding.ASCII.GetString(input).Split("_");
        if (controllerMsg[0].Equals("UNSTUCK"))
        {
            MainThread.wkr.AddJob(() =>
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                player.transform.position = CheckpointsManager.CheckpointList[CheckpointsManager.CurrentCheckpointIndex].transform.position;
                player.transform.forward = CheckpointsManager.CheckpointList[CheckpointsManager.CurrentCheckpointIndex].transform.forward;
                CarUserControl.v = 0;
                CarUserControl.h = 0;
            });
        }
        if (controllerMsg[0].Equals("CONTROLLING"))
        {
            MainThread.wkr.AddJob(() =>
            {
                if (controllerMsg[1].Equals("RUNNING"))
                    CarUserControl.v = .5f;
                else if (controllerMsg[1].Equals("JOGGING"))
                    CarUserControl.v = 0.3f;
                else if (controllerMsg[1].Equals("STOPPING"))
                    CarUserControl.v = 0.1f;

                var turnAngle = float.Parse(controllerMsg[2]);
                CarUserControl.h = turnAngle;
            });
        }
        
    }

    private void StateCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        
    }

    bool IsWatcherStopped() { 
        return watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopped || 
               watcher.Status == BluetoothLEAdvertisementWatcherStatus.Created; 
    }
#endif

    void StartFindingController()
    {
#if ENABLE_WINMD_SUPPORT
        if (bleRadio.State == RadioState.On && IsWatcherStopped()) 
        {
            watcher.Start();
        }
#endif
    }

    void OnApplicationQuit()
    {
#if ENABLE_WINMD_SUPPORT
        if (watcher != null)
        {
            watcher.Stop();
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) {
            isConnected = true;
            switchedScene = true;
            SceneManager.LoadScene(3);
        }

        if (!isConnected && !switchedScene)
        {
            if (connectingPanel.activeInHierarchy && !isConnectingPanelActive)
            {
                connectingPanel.SetActive(false);
            }
            else if (isConnectingPanelActive && !connectingPanel.activeInHierarchy)
            {
                connectingPanel.SetActive(true);
            }

            if (!connectingPanel.activeInHierarchy)
                MainPanelText.text = mainPanelText;
            else
                ConnectingPanelText.text = connectingPanelText;
        }
        else if (isConnected && !switchedScene)
        {
            switchedScene = true;
            SceneManager.LoadScene(3);
        }
    }

    public void BackToMainMenu()
    {
        //ServerManager.Instance.LeaveSession(ServerBase.SESSION_TYPE.LOBBY);
        //Destroy(ServerManager.Instance);
        Destroy(gameObject);
        SceneManager.LoadScene(0);
    }
}
