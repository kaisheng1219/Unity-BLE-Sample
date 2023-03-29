# Unity-BLE-Sample
Unity UWP BLE scripts &amp; Android Plugin sample

## UnityBlePlugin
This folder contains the source codes for the Android BLE plugin

> **Note:** U have to use Android Studio for the following tasks

1. The only folder to consider is the **ble** folder
2. Some things to take note of:
<img src='ReadmeImages/androidStudio-build.png' width='800px'>
<img src='ReadmeImages/androidStudio-gradle.png' width='800px'>
3. Also, from line 126-129

```java
AdvertiseData data = new AdvertiseData.Builder()
                .setIncludeDeviceName( false )
                .addManufacturerData(1, "ctrler".getBytes(StandardCharsets.UTF_8)) // Change "ctrler" to any string that has a length <= 10
                .build();
```

## Unity Android 
This folder contains the sample codes to invoke the methods in the plugin in Unity and also a manifest file to request for certain permissions

> **Note:** GameObjects used in the codes are based on my UI, so please ignore any UI-related codes.
> Also, u have to enable custom manifest file in ur Unity Android build settings

``
Callbacks are executed in a separate thread in Unity, meaning no UI elements can be called in that thread as they only run in the main thread.
To execute UI calls, u may pass the actions into the MainThread class.
``