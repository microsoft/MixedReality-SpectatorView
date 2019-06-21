# ArUco Marker Spatial Alignment Requirements
> Note: this experience does not work with a HoloLens 2 device. When these setup steps were drafted, the opencv dependencies required for ArUco detection were not available for arm/arm64 build flavors.

### HoloLens 1
1. Windows PC
2. HoloLens 1
3. [Visual Studio 2017](https://visualstudio.microsoft.com/vs/) installed on the PC
4. [Unity](https://unity3d.com/get-unity/download) installed on the PC
5. Build [SpectatorView.OpenCV.dll](../src/SpectatorView.Native/SpectatorView.OpenCV/UWP)

### Android
1. Windows PC
2. Android Device that supports [AR Core](https://developers.google.com/ar/discover/supported-devices)
3. [Android Studio](https://developer.android.com/studio)
4. [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0) (Note: only v1.7.0 has been tested, use other versions at your own risk)

## Before building
1. Obtain your HoloLens's ip address from the settings menu via Settings -> Network & Internet -> Wi-Fi -> Hardware Properties.
2. In your Unity project, call Spectator View -> Update All Asset Caches to prepare content for state synchronization.

>> NOTE: Both the HoloLens and android applications should be compiled from the same PC with the same unity project. Updating the asset cache assigns unique identifiers to each item in the unity project. Doing this on different computers can break synchronization.

### HoloLens scene setup
3. Add the [SpectatorView.ArUcoVisual.HoloLens prefab](Prefabs/SpectatorView.ASA.HoloLens.prefab) to the scene you intend to run on the HoloLens device.
4. Add a GameObjectHierarchyBroadcaster to the root game object of the content you want synchronized. 
5. Press the 'HoloLens' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
6. Build and deploy the application to your HoloLens device.

### Android scene setup
7. Open the [SpectatorView.ArUcoVisual.Android unity scene](Scenes/SpectatorView.ASA.Android.unity) in your unity project.
8. Again call Spectator View -> Update All Asset Caches to prepare content for state synchronization.
9. Set the 'User Ip Address' in the Spectator View script to the ip address of your HoloLens device.
10. Press the 'Android' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
11. Check 'ARCore Supported' under Build Settings -> Player Settings -> Android -> XR Settings
12. Build and deploy the application to your android device.

# Example Scenes
* HoloLens: [SpectatorView.ArUcoVisual.HoloLens](Scenes/SpectatorView.ArUcoVisual.HoloLens.unity)
* Android: [SpectatorView.ArUcoVisual.Android](Scenes/SpectatorView.ArUcoVisual.Android.unity)
