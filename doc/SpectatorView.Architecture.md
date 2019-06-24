# Overview

[SpectatorView](Scripts/SpectatorView.cs) is a multi-device experience that allows HoloLens applications to be viewed by additional devices at their own viewpoints. It offers functionality for unidirectional content synchronization (State Synchronization) and leverages spatial coordinates for scene alignment (Spatial Alignment). It can be used to enable a variety of different filming scenarios including prototype documentation and keynote demos.

## Supported filming scenarios
1. Filming with an Android or iOS device
2. Filming with a DSLR camera rig

## Application Flow

### 1. Filming with an Android or iOS device
1. The mobile device begins tracking its location in the world relative to its local application origin using ARCore/ARKit/ARFoundation.
2. The mobile device connects directly to a socket open on the HoloLens device.
3. The mobile device and HoloLens device both locate a [shared spatial coordinate](SpectatorView.SpatialAlignment.md) relative to their local application origins.
4. The mobile device applies a transform to its camera based on the location of this spatial coordinate in the two local application spaces. After applying this transform, the perceived application origin on the mobile device is located in the same physical position as the local application origin on the HoloLens device.
5. The HoloLens begins [sending scene information](SpectatorView.StateSynchronization.md) to the mobile device. The mobile device then updates its local application content to reflect what's being observed on the HoloLens device.
6. The screen of the mobile device can then be [recorded](SpectatorView.Recording.md) to film the HoloLens experience.

### 2. Filming with a DSLR camera rig
1. A HoloLens is mounted to the DSLR camera. [Calibration](SpectatorView.Calibration.md) is then performed to calculate camera intrinsics (Properties such as lens focal length and principal points that are needed for compositing holograms into the DSLR camera feed) and camera extrinsics (The transform of the DSLR camera to the mounted HoloLens). This data is stored on this HoloLens mounted to the DSLR camera.
2. The PC obtains a DSLR camera stream through a capture card.
3. The PC connects to sockets open on both the user HoloLens and DSLR camera rig HoloLens through the unity editor.
4. The PC obtains camera intrinsic and extrinsic information from the DSLR mounted HoloLens and updates its unity camera to reflect these values.
5. The PC instructs both HoloLenses to locate a [shared spatial coordinate](SpectatorView.SpatialAlignment.md).
6. The PC listens to pose updates from the DSLR mounted HoloLens. Using the camera extrinsics, pose updates and original shared coordinate location, the PC updates its unity camera to have its local application origin reflect the user HoloLens's application origin.
7. The user HoloLens begins [sending scene information](SpectatorView.StateSynchronization.md) to the PC. The PC then updates its local application content to reflect what's being observed on the user's HoloLens device.
8. The PC then [composites](SpectatorView.Recording.md) this application content into the DSLR camera stream. This composited content can then be output via the capture card.
9. The composited content can then be [recorded](SpectatorView.Recording.md) to images on the PC that can later be combined into a video.

### Spatial alignment
For more information on spatial alignment, see [SpectatorView.SpatialAlignment](SpectatorView.SpatialAlignment.md)

### State synchronization
For more information on state synchronization, see [SpectatorView.StateSynchronization](SpectatorView.StateSynchronization.md)

### Compositing and Recording
For more information on compositing and recording, see [SpectatorView.Recording](SpectatorView.Recording.md)

### Calibration
For more information on calibration, see [SpectatorView.Calibration](SpectatorView.Calibration.md)
