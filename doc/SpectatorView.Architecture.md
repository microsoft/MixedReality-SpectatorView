# Overview

[SpectatorView](Scripts/SpectatorView.cs) is a multi-device experience that allows HoloLens applications to be viewed by additional devices at their own viewpoints. It offers functionality for unidirectional content synchronization (State Synchronization) and leverages spatial coordinates for scene alignment (Spatial Alignment). It can be used to enable a variety of different filming scenarios including prototype documentation and keynote demos.

## Supported filming scenarios
1. Filming with an Android or iOS device
2. Filming with a DSLR camera rig

## Application Flow

### 1. Filming with an Android or iOS device
1. The mobile device begins tracking its location in the world relative to its local application origin using ARCore/ARKit/ARFoundation.
2. The mobile device connects directly to a socket open on the HoloLens device.
3. The mobile device and HoloLens device both locate a shared spatial coordinate relative to their local application origins.
4. The mobile device applies a transform to its camera based on the location of this spatial coordinate in the two local application spaces. After applying this transform, the perceived application origin on the mobile device is located in the same physical position as the local application origin on the HoloLens device.
5. The HoloLens begins sending scene information to the mobile device. The mobile device then updates content to reflect what's being observed on the HoloLens device.

### 2. Filming with a DSLR camera rig

### Spatial alignment
For more information on spatial alignment, see [SpectatorView.SpatialAlignment](SpectatorView.SpatialAlignment.md)

### State synchronization
For more information on state synchronization, see [SpectatorView.StateSynchronization](SpectatorView.StateSynchronization.md)

### Recording
For more information on recording, see [SpectatorView.Recording](SpectatorView.Recording.md)

### Calibration
For more information on calibration, see [SpectatorView.Calibration](SpectatorView.Calibration.md)
