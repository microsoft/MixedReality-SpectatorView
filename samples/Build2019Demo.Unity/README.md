# Build 2019 Demo

This sample of using SpectatorView is very similar to the live code demo presented at //BUILD 2019 conference ([video link](https://www.youtube.com/watch?v=P8og3nC5FaQ)).

## Contents

The demo consists of a simple experience with a buttons panel and a slider, configured for spectation by another device using SpectatorView functionality. The Unity project structure contains:

- **GoogleARCore:** This folder contains the ARCore SDK referenced from the submodule added at `/external/ARCore-Unity-SDK/`; the source is hosted on [GitHub](https://github.com/google-ar/arcore-unity-sdk).
- **AzureSpatialAnchors:** This folder contains the Azure Spatial Anchors (ASA) plugin referenced from the submodule added at `/external/Azure-Spatial-Anchors-Samples/`; the source is hosted on [GitHub](https://github.com/Azure/azure-spatial-anchors-samples).
- **Demo:** Demo assets and prefabs for the simple experience.
- **MixedReality-SpectatorView:** Spectator View assets symlinked from `/src/SpectatorView.Unity/Assets`
- **MixedRealityToolkit-Unity:** MixedRealityToolkit-Unity assets symlinked from the common submodule located at `/external/MixedRealityToolkit-Unity/Assets`
- **Plugins:** This folder contains parts of the ASA plugin that need to be in this location.

## Running the Demo

In order to run the demo, you will need at least two MR/AR capable devices, with the host device preferably a HoloLens 2. Once you have the devices ready, follow these instructions (below assumes HoloLens 2 for host, and a mobile phone for spectator):

1. Connect your devices to the same WiFi network, and obtain the host device IP Address.
2. Build UWP Player containing `Demo/Scenes/Finished_Scene` scene, and deploy this application to the HoloLens 2host device.
3. `TODO` [Set the IP, ASA keys, etc]
4. Build Android Player containing `MixedReality-SpectatorView/SpectatorView/Scenes/SpectatorView.ASA.Android.unity` scene, and deploy this application to the spectating mobile device. 
    - For iOS, repalce `Android` with `iOS` in the scene path.
5. Launch the `SpectatorView.Build2019Demo` on the HoloLens 2 host, and wait for the experience to start.
    - You can validate the slider and the buttons work.
6. Laucnh the `SpectatorView.Build2019Demo` on the mobile device, and wait for the connection to happen.
