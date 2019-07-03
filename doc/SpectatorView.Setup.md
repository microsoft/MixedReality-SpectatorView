# Spectating with an Android or iOS Device Setup

## Software & Hardware Requirements

### HoloLens 2 & HoloLens Requirements

1. Windows PC
2. HoloLens 2 or HoloLens
3. [Visual Studio 2017](https://visualstudio.microsoft.com/vs/) installed on the PC
4. [Unity](https://unity3d.com/get-unity/download) installed on the PC

### Android Requirements

1. Windows PC
2. Android Device that supports [AR Core](https://developers.google.com/ar/discover/supported-devices)
3. [Android Studio](https://developer.android.com/studio)
4. Obtain [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0) (Note: only v1.7.0 has been tested, use other versions at your own risk). This can be achieved through running the [ResetSamples](../tools/Scripts/ResetSamples.bat) script or by manually copying assets content into the [ARCore-Unity-SDK folder](../external/ARCore-Unity-SDK).

>Note: ARCore does not share MixedReality-SpectatorView's MIT license. For more information on ARCore licensing, see [here](https://github.com/google-ar/arcore-unity-sdk/blob/master/LICENSE).

### iOS Requirements

>Note: ARKit contains some scripts that will generate build failures for HoloLens builds. You will only need to obtain the ARKit Unity Plugin described below on your mac device.

1. Mac
2. ARM64 iOS Device that supports [AR Kit](https://developer.apple.com/library/archive/documentation/DeviceInformation/Reference/iOSDeviceCompatibility/DeviceCompatibilityMatrix/DeviceCompatibilityMatrix.html)
3. [Unity](https://unity3d.com/get-unity/download) installed on the Mac
4. [XCode](https://developer.apple.com/xcode/) installed on the Mac
5. Obtain an [apple developer license](https://developer.apple.com/programs/enroll/)
6. Obtain [Unity's ARKit Plugin](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/src/default/) and place it within the [ARKit-Unity-Plugin folder](../external/ARKit-Unity-Plugin).

>Note: Unity's ARKit Plugin has two licenses, one of which is not a MIT license. For more information on ARKit licensing, see [here](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/src/default/LICENSES/).

## Other Dependencies

Not all spatial alignment strategies support all platforms. See the chart below to determine which strategy best addresses your intended user scenarios.

| Platform  Support      | HoloLens 2 | HoloLens 1 | Android | iOS |
|:----------------------:|:----------:|:----------:|:-------:|:---:|
| Azure Spatial Anchors  | x          | x          | x       | x   |
| QR Code Detection      | x          |            | x       | x   |
| ArUco Marker Detection |            | x          | x       | x   |

### Azure Spatial Anchors

1. Setup an [Azure Spatial Anchors account](https://docs.microsoft.com/en-us/azure/spatial-anchors/quickstarts/get-started-unity-hololens) and obtain the Account Domain, Account ID and the Primary Key.
2. Obtain [AzureSpatialAnchors v1.1.1](https://github.com/Azure/azure-spatial-anchors-samples/releases/tag/v1.1.1). This can be achieved through running the [ResetSamples](../tools/Scripts/ResetSamples.bat) script or by manually copying assets content into the [Azure-Spatial-Anchors-Samples folder](../external/Azure-Spatial-Anchors-Samples).
3. In the Android, iOS and WSA unity player settings, add the **SPATIALALIGNMENT_ASA** preprocessor directive. (This is located via Build Settings -> Player Settings -> Other Settings -> 'Scripting Defined Symbols')
4. When in Unity preparing to build your application, set the Account Domain, Account Id and Account Key for the Spatial Anchors Localizer using values you obtained creating an azure spatial anchors account above. These values are set in the SpatialAnchorsCoordinateLocalizationInitializer MonoBehaviour defined, which will exist for the Android and iOS experiences.

### QR Code Detection

1. Obtain the [MixedReality QR Code Plugin](https://github.com/dorreneb/mixed-reality/releases) and include the associated dll's into the [MixedReality-QRCodePlugin folder](../external/MixedReality-QRCodePlugin).
2. In the WSA unity player settings, add the **QRCODESTRACKER_BINARY_AVAILABLE** preprocessor directive. (This is located via Build Settings -> Player Settings -> Other Settings -> 'Scripting Defined Symbols')

>Note: **QRCODESTRACKER_BINARY_AVAILABLE should be removed from the WSA Player settings when building for HoloLens 1.** Adding QRCODESTRACKER_BINARY_AVAILABLE to the player settings for Android, iOS and HoloLens 2 builds should not generate any issues for other spatial alignment strategies. However, when building for HoloLens 1, the QRCODESTRACKER_BINARY_AVAILABLE needs to be removed from the WSA Player settings. Compilation checks to determine whether a device supports QR Code detection are currently conducted using this QRCODESTRACKER_BINARY_AVAILABLE flag. HoloLens 1 does not support QR Code detection.

### ArUco Marker Detection

1. Build a x86 Release version of [SpectatorView.OpenCV.dll](../src/SpectatorView.Native/README.md) and include the associated dll's in your Unity project. Addint the plugins to your Unity project can be achieved by running the [CopyPluginsToUnity](../tools/Scripts/CopyPluginsToUnity.bat) script.

>Note: No arm version of SpectatorView.OpenCV.dll exists, which will cause build errors for HoloLens 2 devices if these dlls are kept in the project when building for HoloLens 2. It is suggested to delete any SpectatorView.Native directories in the Assets folder when building for HoloLens 2 (This will have been created by the above CopyPluginsToUnity script). When building for HoloLens 1 or running DSLR camera experiences, recopy these dll's to the project.

### Before building

1. Obtain your HoloLens's ip address from the settings menu via Settings -> Network & Internet -> Wi-Fi -> Hardware Properties.
2. Add any of the preprocessor directives or unity packages described above to your clone of the codebase.
3. In your Unity project, call Spectator View -> Update All Asset Caches to prepare content for state synchronization. Commit the updated Generated.StateSynchronization.AssetCaches folder content to your project to share across development devices.

> NOTE: **Asset Caches need to be updated on one development machine and shared across development machines**. Asset Caches aren't currently created in a deterministic manner and can't be recreated in new development environments. The easiest way to share this with a team is to commit changes to the Generated.StateSynchronization.AssetCaches folder that will appear in the Unity project's Assets directory. For more information on Asset Caches see [SpectatorView.StateSynchronization](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/StateSynchronization/README.md).

### HoloLens 2 & HoloLens

1. Make sure your Unity project contains the asset caches that were created in the 'Before building' steps.
2. Add the [SpectatorView prefab](../src/SpectatorView.Unity/Assets/SpectatorView/Prefabs/SpectatorView.prefab) that reflects the correct spatial alignment strategy to the scene you intend to run on the HoloLens device.
3. Add a GameObjectHierarchyBroadcaster to the root game object of the content you want synchronized.
4. Press the 'HoloLens' button on the [Platform Switcher](../src/SpectatorView.Unity/Assets/SpectatorView.Editor/Scripts/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
5. Build and deploy the application to your HoloLens device.

### Android

1. Make sure your Unity project contains the asset caches that were created in the 'Before building' steps.
2. Import [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0).
3. Open the [SpectatorView.Android unity scene](../src/SpectatorView.Unity/Assets/SpectatorView/Scenes/SpectatorView.Android.unity) that reflects the correct spatial alignment strategy in your unity project.
4. Set the 'User Ip Address' in the Spectator View script to the ip address of your HoloLens device.
5. Press the 'Android' button on the [Platform Switcher](../src/SpectatorView.Unity/Assets/SpectatorView.Editor/Scripts/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
6. Check 'ARCore Supported' under Build Settings -> Player Settings -> Android -> XR Settings
7. Export the project to android studio.
8. Update the AndroidManifest.xml in android studio to use the ScreenRecorderActivity class compared to the UnityPlayerActivity as the application activity.
9. Build and deploy the application through android studio to your desired device.

### iOS

> Note: Building iOS applications requires a mac.

1. Make sure your Unity project contains the asset caches that were created in the 'Before building' steps. Asset caches can't be recreated in new development environments, so the asset caches created on the PC need to be checked in or copied over to your mac development environment.
2. Import [Unity's ARKit Plugin](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/src/default/).
3. Open the [SpectatorView.iOS unity scene](../src/SpectatorView.Unity/Assets/SpectatorView/Scenes/SpectatorView.iOS.unity) that reflects the correct spatial alignment strategy in your unity project.
4. Set the 'User Ip Address' in the Spectator View script to the ip address of your HoloLens device.
5. Press the 'iOS' button on the [Platform Switcher](../src/SpectatorView.Unity/Assets/SpectatorView.Editor/Scripts/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
6. Export the iOS project to a XCode solution.
7. Configure the [signing certificate](https://developer.apple.com/support/code-signing/) for your Unity generated project in XCode to reflect your developer account.
8. Build and deploy the application through XCode to your desired device.

## Spectation Scenes

* HoloLens: [SpectatorView.HoloLens](../src/SpectatorView.Unity/Assets/SpectatorView/Scenes/SpectatorView.HoloLens.unity)
* Android: [SpectatorView.Android](../src/SpectatorView.Unity/Assets/SpectatorView/Scenes/SpectatorView.Android.unity)
* iOS: [SpectatorView.iOS](../src/SpectatorView.Unity/Assets/SpectatorView/Scenes/SpectatorView.iOS.unity)
