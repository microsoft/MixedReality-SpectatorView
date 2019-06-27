# Build 2019 Demo

This sample of using SpectatorView is very similar to the live code demo presented at //BUILD 2019 conference.

[![//BUILD 2019 Video](../../doc/images/Build2019DemoVideo.png)](https://www.youtube.com/watch?v=P8og3nC5FaQ "//BUILD 2019 Video")

## Running the Demo

In order to run the demo, you will need at least two MR/AR capable devices, with the host device preferably a HoloLens 2. Once you have the devices ready, follow the instructions below.

> **ARKit Note:** If you wish to run the experience on an iOS device with ARKit, download the [ARKit repository](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/downloads/), unzip it, and copy the contents of the `Unity-Technologies-unity-arkit-plugin-94e47eae5954/Assets` folder to `/samples/Build2019Demo.Unity/Assets/`.

### Instructions

These instructions assumes a HoloLens 2 device for host, and a mobile phone for spectator

1. Connect your devices to the same WiFi network, and obtain the host device IP Address.
2. Build UWP Player containing `Demo/Scenes/Finished_Scene` scene, and deploy this application to the HoloLens 2host device.
3. `TODO` [Set the IP, ASA keys, etc]
4. Build Android Player containing `MixedReality-SpectatorView/SpectatorView/Scenes/SpectatorView.ASA.Android.unity` scene, and deploy this application to the spectating mobile device. 
    - For iOS, repalce `Android` with `iOS` in the scene path.
5. Launch the `SpectatorView.Build2019Demo` on the HoloLens 2 host, and wait for the experience to start.
    - You can validate the slider and the buttons work.
6. Launch the `SpectatorView.Build2019Demo` on the mobile device, and wait for the connection to happen.

## Sample Project Contents

The demo consists of a simple experience with a buttons panel and a slider, configured for spectation by another device using SpectatorView functionality. The Unity project structure contains:

- **ARKit:** This folder is not checked-in by default, but it's needed to run the experience on ARKit iOS device; see the **ARKit Note** above.
- **AzureSpatialAnchors:** This folder contains the Azure Spatial Anchors (ASA) plugin referenced from the submodule added at `/external/Azure-Spatial-Anchors-Samples/`; the source is hosted on [GitHub](https://github.com/Azure/azure-spatial-anchors-samples).
- **Demo:** Demo assets and prefabs for the simple experience.
- **GoogleARCore:** This folder contains the ARCore SDK referenced from the submodule added at `/external/ARCore-Unity-SDK/`; the source is hosted on [GitHub](https://github.com/google-ar/arcore-unity-sdk).
- **MixedReality-SpectatorView:** Spectator View assets symlinked from `/src/SpectatorView.Unity/Assets`
- **MixedRealityToolkit-Unity:** MixedRealityToolkit-Unity assets symlinked from the common submodule located at `/external/MixedRealityToolkit-Unity/Assets`
- **Plugins:** This folder contains parts of the ASA plugin that need to be in this location.
