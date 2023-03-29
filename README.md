# Unity-BLE-Sample
Unity UWP BLE scripts &amp; Android Plugin sample

## UnityBlePlugin
This folder contains the source codes for the Android BLE plugin
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
> **Note:** U have to use Android Studio for this to work