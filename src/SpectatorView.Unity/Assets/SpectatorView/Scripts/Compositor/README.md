## Overview
SpectatorView renders holograms from Unity over a color frame from a capture card.  This uses the calibration data from the calibration app to render the holograms at the correct size and orientation. The Compositor window can save still images or videos to disk, and outputs video to the output port of supported capture cards. Output pictures and videos will be saved to "My Documents\HologramCapture\".

## Setup

### Install SDK and native Unity plugins

Follow the instructions documented in [SpectatorView.Native](SpectatorView.Native.md) for installing the correct SDK for your capture card, and for building and installing the Unity plugins used by the SpectatorView compositor. 

### Build and install the HolographicCamera App

The HolographicCamera app is a UWP application that runs on the HoloLens attached to your video camera. This app reads calibration data stored on the HoloLens and communicates that data to the Unity compositor. This app also transmits the position and rotation of the camera to the Unity compositor.

1. Open the [HolographicCamera.Unity](../src/HolographicCamera.Unity) project in Unity
2. Open the Build window and switch platforms to the Universal Windows Platform
3. Build the Unity project to create a Visual Studio solution
4. Open the generated Visual Studio solution.
5. Change the Solution Configuration to Release and the Architecture to x86.
6. Deploy the application to the HoloLens attached to your video camera.

### Copy calibration data to the HoloLens

Calibration data stores the camera intrinsic information for your video camera, and the camera extrinsic information that represents the positional and rotational offset between the HoloLens and the video camera.

1. Follow the steps at [SpectatorView.Calibration](SpectatorView.Calibration.md) to produce a calibration file for your camera and HoloLens.
2. Use the [Device Portal](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal-hololens) to connect to the HoloLens attached to your camera.
3. On the File Explorer tab in the device portal, upload the calibration file to the Pictures library. The final file path should be "User Folders\Pictures\CalibrationData.json".

## Running the compositor

Follow the instructions at [SpectatorView.Setup](SpectatorView.Setup.md) to include SpectatorView support in your application. Once you have the HolographicCamera app installed and running and SpectatorView support enabled in your application and running, you are ready to run the compositor.

### Start the Compositor
1. Open the [SpectatorViewCompositor](../src/SpectatorView.Unity/Assets/SpectatorView.Editor/Scenes/SpectatorViewCompositor.unity) scene in your application's Unity project (with your build platform set to Universal Windows Platform).
2. Open the Compositor Window from the Spectator View -> Compositor menu.
3. Press Play to run the scene.

You should now see video output from your camera in the Compositor window. If you don't, here are some troubleshooting suggestions:
+ Make sure the camera is turned on and that the lens cap is off.
+ Make sure the camera is connected to the input port of the capture card.
+ Try using software for your capture card (e.g. Blackmagic Media Express) to validate that camera input is coming into your computer.

### Connect the compositor to the HolographicCamera
Connecting the compositor to the HolographicCamera app running on your camera's HoloLens will provide calibration and pose information for the camera to the compositor.

1. Find the IP address for your HoloLens and enter it inside the first text area in the Holographic Camera box inside the Compositor window.
2. Launch the HolographicCamera (SpectatorView.HolographicCamera) app on the camera's HoloLens.
3. Click the "Connect" button inside the Compositor window's Holographic Camera box.

You should now see the status change from "Not connected" to "Connected to &lt;your HoloLens name&gt; (&lt;your HoloLens IP address&gt;)". You should also see the Calibration status change from "Not loaded" to "Loaded".

### Locate a shared spatial coordinate for the HolographicCamera
Shared spatial coordinates let the HoloLens on the camera and the HoloLens running your application refer to real-world coordinates in a coordinate space shared between both devices. See [SpectatorView.SpatialAlignment](SpectatorView.SpatialAlignment.md) for more information about shared spatial coordinates.

In Preview the Compositor window uses an ArUco marker detector to detect ArUco marker 0 at a printed size of 10cm by 10cm. In future releases, the Compositor window will allow you to choose which type of spatial coordinate sharing to use.

1. Click the "Located Shared Spatial Coordinate" button inside the Holographic Camera box. This should cause the video camera light on your HoloLens to come on, and cause the HoloLens to start searching fo the ArUco marker.
2. Aim the HoloLens at the ArUco marker you've printed out. Once the HoloLens detects the marker, the Compositor window's status should change from "Locating shared spatial coordinate..." to "Located".

### Connect the compositor to your application
The steps for connecting the Compositor to your application are the same as for the HolographicCamera app. Once your application is connected to the compositor, you should start to see your application's content appear in the Unity editor through the [State Synchronization](SpectatorView.StateSynchronization.md) system. Once the shared spatial coordinate is located for both the HolographicCamera and for your application, the content from your application should appear on top of the real-world video in the same location as it does in the application on your HoloLens.

## Options and configuration
### Recording
The **Recording** expander in the Compositor window can be used to start and stop recording or to take a still picture. Videos and pictures are saved in your Documents\HologramCapture directory. By default, audio from your computer's microphone will be recorded as part of the video.

The compositor also outputs video to your capture card (for cards that support output).

### Settings
The **Hologram Settings** expander lets you configure the following options.

**Video source** - before you run the Compositor scene, you can choose which capture card should be used as the input video source. The source cannot be changed once you've started video playback.

**Alpha** - allows you to change the opacity of holograms as they're rendered on top of video. A value of 1 will make holograms completely opaque, while 0 will make holograms completely transparent.

**Frame time adjustment** - provides a way to manually adjust for the latency in the capture card and in the network traffic between the HolographicCamera app and the compositor. When you move the camera, if the holograms seem to lag behind or follow ahead of the real world, adjust this slider to correct for the latency.

### Compositor stats
When the compositor is running, the **Compositor Stats** expander shows you statistics about composition.

**Framerate** - this framerate box shows the minimum, maximum, and average framerates over the previous second. In order to maintain uninterrupted output of video at 60 frames a second, your scene must render 60 frames a second on average. If you consistently render under 60 frames a second, you may need to make performance tradeoffs in your scene (e.g. decreasing render quality) in order to record smooth video

**Queued output frames** - Shows how many already-rendered frames are queued for output to the capture card. This buffer is used to ensure that video frame output remains smooth over brief frame hitches in your scene. If your scene consistently runs under the target framerate or consistently experiences hitches, the video stream will eventually hitch once this buffer is depleted.