# QR Code Spatial Alignment Requirements
> Note: QR Code based localization is only supported for HoloLens 2. This setup process won't break the ability to compile for HoloLens 1 devices, but it also won't enable QR code based localization for HoloLens 1 devices.

### HoloLens 2
1. Windows PC
2. HoloLens 2
3. [Visual Studio 2017](https://visualstudio.microsoft.com/vs/) installed on the PC
4. [Unity](https://unity3d.com/get-unity/download) installed on the PC
5. [MixedReality QR Code Plugin](https://github.com/dorreneb/mixed-reality/releases)

### Android
1. Windows PC
2. Android Device that supports [AR Core](https://developers.google.com/ar/discover/supported-devices)
3. [Android Studio](https://developer.android.com/studio)
4. [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0) (Note: only v1.7.0 has been tested, use other versions at your own risk)

## Before building
1. Obtain your HoloLens's ip address from the settings menu via Settings -> Network & Internet -> Wi-Fi -> Hardware Properties.
2. In the WSA unity player settings, add the **QRCODESTRACKER_BINARY_AVAILABLE** preprocessor directive. (This is located via Build Settings -> Player Settings -> Other Settings -> 'Scripting Defined Symbols')
3. In your Unity project, call Spectator View -> Update All Asset Caches to prepare content for state synchronization.

> NOTE: Both the HoloLens 2 and android applications should be compiled from the same PC with the same unity project. Updating the asset cache assigns unique identifiers to each item in the unity project. Doing this on different computers can break synchronization.

### HoloLens scene setup
4. Add the [SpectatorView.QRCodeVisual.HoloLens prefab](Prefabs/SpectatorView.QRCodeVisual.HoloLens.prefab) to the scene you intend to run on the HoloLens device.
5. Add a GameObjectHierarchyBroadcaster to the root game object of the content you want synchronized. 
6. Press the 'HoloLens' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
7. Build and deploy the application to your HoloLens device.

### Android scene setup
8. Open the [SpectatorView.QRCodeVisual.Android unity scene](Scenes/SpectatorView.ASA.Android.unity) in your unity project.
9. Again call Spectator View -> Update All Asset Caches to prepare content for state synchronization.
10. Set the 'User Ip Address' in the Spectator View script to the ip address of your HoloLens device.
11. Press the 'Android' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
12. Check 'ARCore Supported' under Build Settings -> Player Settings -> Android -> XR Settings
13. Build and deploy the application to your android device.

# Example Scenes
* HoloLens: [SpectatorView.QRCodeVisual.HoloLens](Scenes/SpectatorView.QRCodeVisual.HoloLens.unity)
* Android: [SpectatorView.QRCodeVisual.Android](Scenes/SpectatorView.QRCodeVisual.Android.unity)
