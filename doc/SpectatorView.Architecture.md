# Spectator View Architecture

`SpectatorView` is a multi-device experience that allows HoloLens applications to be viewed by additional devices at their own viewpoints. It offers functionality for unidirectional content synchronization (State Synchronization) and leverages spatial coordinates for scene alignment (Spatial Alignment). It can be used to enable a variety of different filming scenarios including prototype documentation and keynote demos.

## Supported scenarios

1. Spectating with an Android or iOS device
2. Spectating with a video camera rig

## Application Flow

### 1. Spectating with an Android or iOS device

1. The mobile device begins tracking its location in the world relative to its local application origin using ARCore\ARKit\ARFoundation.
2. The mobile device connects directly to a socket open on the HoloLens device.
3. The mobile device and HoloLens device both locate a [shared spatial coordinate](../src/SpectatorView.Unity/Assets/SpatialAlignment/README.md) relative to their local application origins.
4. The mobile device applies a transform to its camera based on the location of this spatial coordinate in the two local application spaces. After applying this transform, the perceived application origin on the mobile device is located in the same physical position as the local application origin on the HoloLens device.
5. The HoloLens begins [sending scene information](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/StateSynchronization/README.md) to the mobile device. The mobile device then updates its local application content to reflect what's being observed on the HoloLens device.
6. The screen of the mobile device can then be [recorded](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/README.md) to film the HoloLens experience.

### 2. Spectating with a video camera rig

1. A HoloLens is mounted to the video camera. [Calibration](SpectatorView.Setup.VideoCamera.md#calibration) is then performed to calculate camera intrinsics (Properties such as lens focal length and principal points that are needed for compositing holograms into the video camera feed) and camera extrinsics (The transform of the video camera to the mounted HoloLens). This data is stored on this HoloLens mounted to the video camera.
2. The PC obtains a video camera stream through a capture card.
3. The PC connects to sockets open on both the user HoloLens and video camera rig HoloLens through the unity editor.
4. The PC obtains camera intrinsic and extrinsic information from the DSLR mounted HoloLens and updates its unity camera to reflect these values.
5. The PC instructs both HoloLenses to locate a [shared spatial coordinate](../src/SpectatorView.Unity/Assets/SpatialAlignment/README.md).
6. The PC listens to pose updates from the DSLR mounted HoloLens. Using the camera extrinsics, pose updates and original shared coordinate location, the PC updates its unity camera to have its local application origin reflect the user HoloLens's application origin.
7. The user HoloLens begins [sending scene information](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/StateSynchronization/README.md) to the PC. The PC then updates its local application content to reflect what's being observed on the user's HoloLens device.
8. The PC then [composites](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/README.md) this application content into the video camera stream. This composited content can then be output via the capture card.
9. The composited content can then be [recorded](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/README.md) to images on the PC that can later be combined into a video.

### Spatial alignment

For more information on spatial alignment, see [here](../src/SpectatorView.Unity/Assets/SpatialAlignment/README.md).

### State synchronization

For more information on state synchronization, see [here](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/StateSynchronization/README.md).

### Screen Recording

For more information on compositing and recording, see [here](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/README.md).

### Video Camera Recording

For more information on recording with a video camera, see [here](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/Compositor/README.md).

### Customizing UI

For more information on creating and customizing UI, see [here](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/UI/README.md).