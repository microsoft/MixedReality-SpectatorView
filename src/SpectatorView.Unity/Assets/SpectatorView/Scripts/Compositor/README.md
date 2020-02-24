# Overview

| Supported Functionality  |                                                                |
|:-------------------------|:--------------------------------------------------------------:|
| Number of Video Cameras: | 1                                                              |
| Input Resolution:        | 1920x1080 (1080p)                                              |
| Output Resolution:       | 1920x1080 (1080p)                                              |
| Recording Resolutions:   | 1920x1080 (1080p) Hologram Composited Frame, 4K Quadrant Frame (see note) |
| Capture Cards:           | Blackmagic Design Intensity Pro 4K, Elgato HD 60S, Azure Kinect DK                         |
| Platform:                | PC                                                             |

>Note: A 4K Quadrant Frame does not generate a 4K recording of the hologram composited feed. It actually contains four streams that are each 1920x1080: The unprocessed video camera feed, the hologram feed on a black background, the hologram alpha channel feed and the composited hologram feed.

Spectator View renders holograms from Unity over a color frame from a capture card.  This uses the calibration data from the calibration app to render the holograms at the correct size and orientation. The Compositor window can save still images or videos to disk, and outputs video to the output port of supported capture cards. Output pictures and videos will be saved to "My Documents\HologramCapture\".

## Setup

### Install SDK and native Unity plugins

Follow the instructions documented in [SpectatorView.Native](../../../../../SpectatorView.Native/README.md) for installing the correct SDK for your capture card, and for building and installing the Unity plugins used by the SpectatorView compositor.

>Note: The following steps are setup specific to using a video camera with a mounted HoloLens. If using any other capture device (ie. Azure Kinect) skip to "Running the Compositor". 
### Build and install the HolographicCamera App

The HolographicCamera app is a UWP application that runs on the HoloLens 2 attached to your video camera. This app reads calibration data stored on the HoloLens and communicates that data to the Unity compositor. This app also transmits the position and rotation of the camera to the Unity compositor. If you have gone through the calibration process described [here](../../../../../../doc/SpectatorView.Setup.VideoCamera.md), you may already have this application installed on your device.

1. Open the `src/HolographicCamera.Unity` project in Unity.
2. Open the Build window and switch platforms to the Universal Windows Platform.
3. Build the Unity project to create a Visual Studio solution.
4. Open the generated Visual Studio solution.
5. Change the Solution Configuration to Release and the Architecture to ARM.
6. Deploy the application to the HoloLens 2 attached to your video camera.

### Copy calibration data to your HoloLens 2

Calibration data stores the camera intrinsic information for your video camera, and the camera extrinsic information that represents the positional and rotational offset between the HoloLens and the video camera. If you uploaded a CalibrationData.json file when calibrating the video camera rig, you can skip the steps below. However, filming won't be possible if a CalibrationData.json file isn't placed in the HoloLens 2's Pictures folder.

1. Follow the steps [here](../../../../../../doc/SpectatorView.Setup.VideoCamera.md) to produce a calibration file for your camera and HoloLens.
2. Use the [Device Portal](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal-hololens) to connect to the HoloLens attached to your camera.
3. On the File Explorer tab in the device portal, upload the calibration file to the Pictures library. The final file path should be "User Folders\Pictures\CalibrationData.json".

## Running the compositor

Follow the instructions at [SpectatorView.Setup](../../../../../../doc/SpectatorView.Setup.md) to include SpectatorView support in your application. Once you have the HolographicCamera app installed and running and SpectatorView support enabled in your application and running, you are ready to run the compositor.

### Start the Compositor

1. Open the `SpectatorViewCompositor` scene in your application's Unity project (with your build platform set to Universal Windows Platform).
2. Open the Compositor Window from the Spectator View -> Compositor menu.
3. Select your desired Video Source from the dropdown list. 
4. If the selected Video Source enables depth-based occlusion, select desired occlusion mode.

>Note: "RawDepthCamera" occludes utilizing only the depth image provided by the camera. "BodyTracking" combines the depth image and body mask provided by Azure Body Tracking SDK to occlude only recognized people.

6. Select your desired preview display.
>Note: Depth-based occlusion masks are not included in "intermediate textures" preview display type.
5. Press Play to run the scene.

>Note: Azure Kinect Body Tracking SDK has dependencies ("dnn_model_2_0.onnx","k4abt.dll", "onnxruntime.dll", "cublas64_100.dll", "cudart64_100.dll", "cudnn64_7.dll") that must be located in the same folder as your Unity executable. If body tracking based occlusion is selected and these dependencies are not located in the correct folder, a button enabling the copy of these dependencies will appear and must be executed prior to playing the scene. 

You should now see video output from your camera in the Compositor window. If you don't, here are some troubleshooting suggestions:

- Make sure the camera is turned on and that the lens cap is off.
- Make sure the camera is connected to the input port of the capture card.
- Make sure the firmware is up to date on the Azure Kinect (https://docs.microsoft.com/en-us/azure/kinect-dk/update-device-firmware)
- Try using software for your capture card (e.g. Blackmagic Media Express, K4aviewer.exe) to validate that camera input is coming into your computer.

### Connect the compositor to the HolographicCamera

**Instructions for HoloLens-based camera**

Connecting the compositor to the HolographicCamera app running on your camera's HoloLens will provide calibration and pose information for the camera to the compositor.

1. Find the IP address for your HoloLens and enter it inside the first text area in the Holographic Camera box inside the Compositor window.
2. Launch the HolographicCamera (SpectatorView.HolographicCamera) app on the camera's HoloLens.
3. Click the "Connect" button inside the Compositor window's Holographic Camera box.

You should now see the status change from "Not connected" to "Connected to &lt;your HoloLens name&gt; (&lt;your HoloLens IP address&gt;)". You should also see the Calibration status change from "Not loaded" to "Loaded".

**Instructions for the Azure Kinect camera**

The Azure Kinect camera will automatically be connected when you start the compositor, and will restore any previously-located position for the camera's world pose.

### Locate a shared spatial coordinate for the HolographicCamera

**Instructions for HoloLens-based camera** 

Shared spatial coordinates let the HoloLens on the camera and the HoloLens running your application refer to real-world coordinates in a coordinate space shared between both devices. See [SpectatorView.SpatialAlignment](../../../SpatialAlignment/README.md) for more information about shared spatial coordinates.

The Compositor window allows you to choose which type of spatial coordinate sharing to use. From the Spatial Alignment drop-down, choose the type of spatial alignment strategy used. Some strategies support additional options from the gear drop-down to the right of the box.

1. Choose the type of spatial alignment strategy from the drop-down within the Holographic Camera box.
1. Click the "Located Shared Spatial Coordinate" button inside the Holographic Camera box. This should cause the video camera light on your HoloLens to come on, and cause the HoloLens to start searching for the coordinate.
2. For marker-based strategies, aim the HoloLens at the marker you've printed out. Once the HoloLens detects the marker, the Compositor window's status should change from "Locating shared spatial coordinate..." to "Located".

**Instructions for Azure Kinect camera**

The Shared Spatial Coordinate feature allows placing the stationary Azure Kinect camera relative to a stationary marker. Azure Kinect supports locating an ArUco marker as its only supported strategy.

1. Choose ArUco as the spatial alignment strategy from the drop-down within the Holographic Camera box.
2. Place an ArUco marker within view of the Azure Kinect camera.
3. Click the "Located Shared Spatial Coordinate" button inside the Holographic Camera box.
4. Once the marker detector has located the ArUco marker, the status should change from "Locating shared spatial coordinate..." to "Located".

### Connect the compositor to your application

The steps for connecting the Compositor to your application are the same as for the HolographicCamera app. Once your application is connected to the compositor, you should start to see your application's content appear in the Unity editor through the [State Synchronization](../StateSynchronization/README.md) system. Once the shared spatial coordinate is located for both the HolographicCamera and for your application, the content from your application should appear on top of the real-world video in the same location as it does in the application on your HoloLens.

> Note: If you consistently see an `Unknown Tracking state` message in the compositor window for your connected HoloLens, you may need to add a `HoloLensTrackingObserver` to your Unity scene.

## Options and configuration

### Recording

The **Recording** expander in the Compositor window can be used to start and stop recording or to take a still picture. Videos and pictures are saved in your Documents\HologramCapture directory. By default, audio from your computer's microphone will be recorded as part of the video.

The **Video output mode** option allows you to choose between **Normal** mode, which records only the final composited video, or **Split channels**, which records a split view with the original video, the opaque hologram without background, the alpha mask for the holograms, and the final composited video.

The compositor also outputs video to your capture card (for cards that support output). The output to your video card is always the final composited video.

### Settings

The **Hologram Settings** expander lets you configure the following options.

**Video source** - before you run the Compositor scene, you can choose which capture card should be used as the input video source. The source cannot be changed once you've started video playback.

**Alpha** - allows you to change the opacity of holograms as they're rendered on top of video. A value of 1 will make holograms completely opaque, while 0 will make holograms completely transparent.

**Frame time adjustment** - provides a way to manually adjust for the latency in the capture card and in the network traffic between the HolographicCamera app and the compositor. When you move the camera, if the holograms seem to lag behind or follow ahead of the real world, adjust this slider to correct for the latency.

### Compositor stats

When the compositor is running, the **Compositor Stats** expander shows you statistics about composition.

**Framerate** - this framerate box shows the minimum, maximum, and average framerates over the previous second. In order to maintain uninterrupted output of video at 60 frames a second, your scene must render 60 frames a second on average. If you consistently render under 60 frames a second, you may need to make performance tradeoffs in your scene (e.g. decreasing render quality) in order to record smooth video

**Queued output frames** - Shows how many already-rendered frames are queued for output to the capture card. This buffer is used to ensure that video frame output remains smooth over brief frame hitches in your scene. If your scene consistently runs under the target framerate or consistently experiences hitches, the video stream will eventually hitch once this buffer is depleted.

## Troubleshooting
For up to date information on troubleshooting filming issues, see [here](../../../../../../doc/SpectatorView.Setup.VideoCamera.md).