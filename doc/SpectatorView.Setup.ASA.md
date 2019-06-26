# Requirements
### HoloLens 2 & HoloLens
1. Windows PC
2. HoloLens 2 or HoloLens
3. [Visual Studio 2017](https://visualstudio.microsoft.com/vs/) installed on the PC
4. [Unity](https://unity3d.com/get-unity/download) installed on the PC
5. [AzureSpatialAnchors v1.1.1](https://github.com/Azure/azure-spatial-anchors-samples/releases/tag/v1.1.1)

### Android
1. Windows PC
2. Android Device that supports [AR Core](https://developers.google.com/ar/discover/supported-devices)
3. [Android Studio](https://developer.android.com/studio)
4. [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0) (Note: only v1.7.0 has been tested, use other versions at your own risk)
5. [AzureSpatialAnchors v1.1.1](https://github.com/Azure/azure-spatial-anchors-samples/releases/tag/v1.1.1)

### iOS
1. Mac
2. ARM64 iOS Device that supports [AR Kit](https://developer.apple.com/library/archive/documentation/DeviceInformation/Reference/iOSDeviceCompatibility/DeviceCompatibilityMatrix/DeviceCompatibilityMatrix.html)
3. [Unity](https://unity3d.com/get-unity/download) installed on the Mac
4. [XCode](https://developer.apple.com/xcode/) installed on the Mac
5. Obtain an [apple developer license](https://developer.apple.com/programs/enroll/)
6. [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/manual/index.html) Unity Package
7. [ARKit XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arkit@1.0/manual/index.html) Unity Package


## Before building
1. Obtain your HoloLens's ip address from the settings menu via Settings -> Network & Internet -> Wi-Fi -> Hardware Properties.
2. Setup an [Azure Spatial Anchors account](https://docs.microsoft.com/en-us/azure/spatial-anchors/quickstarts/get-started-unity-hololens) and obtain the Account Domain, Account ID and the Primary Key.
3. Import [AzureSpatialAnchors v1.1.1](https://github.com/Azure/azure-spatial-anchors-samples/releases/tag/v1.1.1) to your Unity project.
4. In both the Android, iOS and WSA unity player settings, add the SPATIALALIGNMENT_ASA preprocessor directive. (This is located via Build Settings -> Player Settings -> Other Settings -> 'Scripting Defined Symbols')
5. In your Unity project, call Spectator View -> Update All Asset Caches to prepare content for state synchronization.

> NOTE: **Asset Caches need to be updated on one development machine and shared across development machines**. Asset Caches aren't currently created in a deterministic manner and can't be recreated in new development environments. The easiest way to share this with a team is to commit changes to the Generated.StateSynchronization.AssetCaches folder that will appear in the Unity project's Assets directory. 

### HoloLens scene setup
6. Add the [SpectatorView.ASA.HoloLens prefab](Prefabs/SpectatorView.ASA.HoloLens.prefab) to the scene you intend to run on the HoloLens device.
7. Add a GameObjectHierarchyBroadcaster to the root game object of the content you want synchronized. 
8. In the unity inspector, set 'Broadcasted Content' in the Spectator View script to be the root game object that now contains the GameObjectHierarchyBroadcaster.
9. Add a parent game object to your unity camera.
10. In the unity inspector, set 'Parent Of Main Camera' in the Spectator View script to be the parent game object you just created.
11. In the unity inspector, set the Account Domain, Account Id and Account Key for the Spatial Anchors Localizer using values you obtained creating an azure spatial anchors account above.
12. Press the 'HoloLens' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
13. Build and deploy the application to your HoloLens device.

### Android scene setup
1. Import [ARCore v1.7.0](https://github.com/google-ar/arcore-unity-sdk/releases/tag/v1.7.0)
14. Open the [SpectatorView.ASA.Android unity scene](Scenes/SpectatorView.ASA.Android.unity) in your unity project.
15. Again call Spectator View -> Update All Asset Caches to prepare content for state synchronization.
16. Set the 'User Ip Address' in the Spectator View script to the ip address of your HoloLens device.
17. In the unity inspector, set the Account Domain, Account Id and Account Key for the Spatial Anchors Localizer using values you obtained creating an azure spatial anchors account above.
18. Press the 'Android' button on the [Platform Switcher](Scripts/Editor/PlatformSwitcherEditor.cs) attached to Spectator View in the unity inspector (This should configure the correct build settings and app capabilities).
19. Check 'ARCore Supported' under Build Settings -> Player Settings -> Android -> XR Settings
20. Build and deploy the application to your android device.

## iOS
For iOS, you will need a Mac with both Unity and XCode installed. After opening your Unity project, Install the [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/manual/index.html) and [ARKit XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arkit@1.0/manual/index.html) Unity Packages using Unity's Package Manager. Then, use the [PlatformSwitcherEditor](xref:Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Editor.PlatformSwitcherEditor) component to configure the project for iOS. Then select File -> Build Settings -> Build. You can then open the generated solution in XCode to compile and deploy.

![Spectator View iOS Build Settings](https://github.com/Microsoft/MixedRealityToolkit-Unity/blob/wikiFiles/Documentation/images/spectatorViewIOSBuildSettings.png)

Within XCode, you will need to configure a [signing certificate](https://developer.apple.com/support/code-signing/) for your application before compiling and deploying. You can do this by selecting your Unity generated XCode project and updating the signing information.

# Example Scenes
* HoloLens: [SpectatorView.ASA.HoloLens](Scenes/SpectatorView.ASA.HoloLens.unity)
* Android: [SpectatorView.ASA.Android](Scenes/SpectatorView.ASA.Android.unity)
* iOS: [SpectatorView.ASA.iOS](Scenes/SpectatorView.ASA.iOS.unity)
