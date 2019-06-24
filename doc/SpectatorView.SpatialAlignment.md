# Spatial Alignment

# Platform Support
Not all spatial alignment strategies support all platforms. See the chart below to determine which strategy best addresses your intended user scenarios.

| Platform  Support      | HoloLens 2 | HoloLens 1 | Android | iOS |
|:----------------------:|:----------:|:----------:|:-------:|:---:|
| Azure Spatial Anchors  | x          | x          | x       | x   |
| QR Code Detection      | x          |            | x       | x   |
| ArUco Marker Detection |            | x          | x       | x   |

# Application Flow
1. On startup, SpatialLocalizers declared in the scene register themselves with the SpatialCoordinateSystemManager. Each SpatialLocalizer enables a different spatial alignment strategy and has a unique id so that it can be invoked through local or remote spatial alignment requests.

2. The SpatialCoordinateSystemManager then listens for network connections (This is facilitated through state synchronization's ICommandRegistry). For each connection, the SpatialCoordinateSystemManager creates a SpatialCoordinateSystemParticipant, which is responsible for monitoring and reporting coordinate state across devices. The SpatialCoordinateSystemParticipant also caches the associated network socket to allow for sending messages across devices during the spatial alignment process.

3. Depending on the experience, the SpatialCoordinateSystemManager is directed to create a LocalizationSession for a specific SpatialLocalizer. For mobile experiences, the mobile device typically has a LocalizationInitializer MonoBehaviour declared in its scene that chooses the correct SpatialLocalizer and starts a localization session for itself as well as the user HoloLens upon creation of a SpatialCoordinateSystemParticipant. For DSLR filming, the compositor window in the Unity editor tells both the user HoloLens and HoloLens mounted to the DSLR camera when to localize. LocalizationSession logic varies for different spatial alignment strategies. A more in-depth look at each alignment strategy can be found below.

4. Once the LocalizationSession has completed, a SpatialCoordinate will have been located. The position of this SpatialCoordinate in the local application space is then cached in the SpatialCoordinateSystemParticipant. The SpatialCoordinateSystemParticipant then relays this information to its associated peer device. This allows both devices to know the location of the SpatialCoordinate in each others' local application spaces.

5. Once the SpatialCoordinate's location and orientation are known for both devices, the spectator device's camera is updated so that the observed content has a physical origin that's at the same location as the user device's local application origin. For mobile devices, this is achieved through the SpatialCoordinateTransformer class that applies an additional transform to the mobile device's tracking camera. For DSLR filming, the editor combines reported head pose information with this SpatialCoordinate information to update its camera.
> Note: Spatial alignment relies on updating the unity camera transform on spectator devices for aligning experiences in the physical world. Its possible to move application content instead of transforming unity cameras to align scene content in the physical world. However, transforming application content can make for a more difficult time synchronizing physics state information across devices, so its suggested to update the camera transform.

# Spatial Alignment Strategies

### Azure Spatial Anchors
Coming soon...

### Marker Visuals and Marker Detection (QR Codes and ArUco Markers)
Spatial alignment based on marker visuals and marker detection allows spectator mobile devices to align with a user HoloLens device. Different marker detectors may be used in the experience, but the general application flow is provided below:

1. Using a LocalizationInitializer, the mobile device instructs the user HoloLens device to create a LocalizationSession for a MarkerVisualDetectorSpatialLocalizer. This requires populating and sending SpatialLocalizationSettings.

2. After telling the user HoloLens device to localize, the mobile device creates its own LocalizationSession using a MarkerVisualSpatialLocalizer. This again requires populating SpatialLocalizationSettings.

3. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device tells the user HoloLens device what marker ids are supported by its marker visual.

4. In the MarkerVisualDetectorSpatialLocalizer LocalizationSession, the user HoloLens device assigns the mobile device a marker id. The user HoloLens then begins marker detection by starting coordinate discovery for the MarkerDetectorCoordinateService.

5. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device receives its assigned marker id. It then shows a marker visual by starting coordinate discovery for the MarkerVisualCoordinateService.

6. Once the user HoloLens has detected the marker being displayed on the mobile device, a SpatialCoordinate is created and the mobile device is told that the marker visual has been found. The creation of this SpatialCoordinate completes the LocalizationSession on the user HoloLens.

7. Once informed that the marker has been found, the mobile device creates a SpatialCoordinate that reflects the marker visual's location at the time of detection. The creation of this SpatialCoordinate completes the Localizationsession on the mobile device.

8. The SpatialCoordinate locations found on both devices are then shared with one another, which allows for the scene to be aligned. 

### Physical Marker Detection  (QR Codes and ArUco Markers)
Spatial alignment based on physical marker detection allows a spectator HoloLens device to align with a user HoloLens device.

# Setup
Different spatial alignment strategies require different external dependencies. Below, you can find setup steps for each of the supported localization strategy.

1. [Azure Spatial Anchors Setup](SpectatorView.Setup.ASA.md)
2. [QR Code Detection](SpectatorView.Setup.QRCode.md)
3. [ArUco Marker Detection](SpectatorView.Setup.ArUcoMarker.md)
