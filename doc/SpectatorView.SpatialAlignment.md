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

3. Depending on the experience, the SpatialCoordinateSystemManager is directed to create a LocalizationSession for a specific SpatialLocalizer. For experiences with the Spectator role chosen on the SpectatorView component, a SpatialLocalizationInitializer that controls localization is chosen from one of two locations. The experience can choose a preferred list of SpatialLocalizationInitializers on the SpectatorViewSettings prefab. In Unity, the Spectator View / Edit Settings menu option will generate this prefab if it does not exist. On the SpatialLocalizationInitializationSettings component, the PrioritizedInitializers list can specify one or more SpatialLocalizationInitializer components. At runtime, the first SpatialLocalizationInitializer that is supported by both devices will be chosen and used to start spatial alignment. If no app-specific SpatialLocalizationInitializer has been specified on the SpectatorViewSettings prefab, a default set specified by the SpectatorView prefab will be consulted in the same manner. For DSLR filming, the compositor window in the Unity editor tells both the user HoloLens and HoloLens mounted to the DSLR camera when to localize. LocalizationSession logic varies for different spatial alignment strategies. A more in-depth look at each alignment strategy can be found below.

4. Once the LocalizationSession has completed, a SpatialCoordinate will have been located. The position of this SpatialCoordinate in the local application space is then cached in the SpatialCoordinateSystemParticipant. The SpatialCoordinateSystemParticipant then relays this information to its associated peer device. This allows both devices to know the location of the SpatialCoordinate in each others' local application spaces.

5. Once the SpatialCoordinate's location and orientation are known for both devices, the spectator device's camera is updated so that the observed content has a physical origin that's at the same location as the user device's local application origin. For mobile devices, this is achieved through the SpatialCoordinateTransformer class that applies an additional transform to the mobile device's tracking camera. For DSLR filming, the editor combines reported head pose information with this SpatialCoordinate information to update its camera.
> Note: Spatial alignment relies on updating the Unity camera transform on spectator devices for aligning experiences in the physical world. Its possible to move application content instead of transforming Unity cameras to align scene content in the physical world. However, transforming application content can make for a more difficult time synchronizing physics state information across devices, so its suggested to update the camera transform.

# Spatial Alignment Strategies

### Azure Spatial Anchors
Coming soon...

### Marker Visuals and Marker Detection (QR Codes and ArUco Markers)
Spatial alignment based on marker visuals and marker detection allows spectator mobile devices to align with a user HoloLens device. Different marker detectors may be used in the experience (QR Code detection is supported for HoloLens 2, while ArUco marker detection is supported for HoloLens 1), but the general application flow is provided below:

1. Using a LocalizationInitializer, the mobile device instructs the user HoloLens device to create a LocalizationSession for a MarkerVisualDetectorSpatialLocalizer. This requires populating and sending SpatialLocalizationSettings.

2. After telling the user HoloLens device to localize, the mobile device creates its own LocalizationSession using a MarkerVisualSpatialLocalizer. This again requires populating SpatialLocalizationSettings.

3. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device tells the user HoloLens device what marker ids are supported by its marker visual.

4. In the MarkerVisualDetectorSpatialLocalizer LocalizationSession, the user HoloLens device assigns the mobile device a marker id. The user HoloLens then begins marker detection by starting coordinate discovery for the MarkerDetectorCoordinateService.

5. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device receives its assigned marker id. It then shows a marker visual by starting coordinate discovery for the MarkerVisualCoordinateService.

6. Once the user HoloLens has detected the marker being displayed on the mobile device, a SpatialCoordinate is created and the mobile device is told that the marker visual has been found. The creation of this SpatialCoordinate completes the LocalizationSession on the user HoloLens.

7. Once informed that the marker has been found, the mobile device creates a SpatialCoordinate that reflects the marker visual's location at the time of detection. The creation of this SpatialCoordinate completes the Localizationsession on the mobile device.

8. The SpatialCoordinate locations found on both devices are then shared with one another, which allows for the scene to be aligned. 

### Physical Marker Detection  (QR Codes and ArUco Markers)
Spatial alignment based on physical marker detection allows a spectator HoloLens device to align with a user HoloLens device. Again, different marker detectors may be used in this experience, but the application flow is the following:

1. The SpatialCoordinateSystemManager is told to start localization using a MarkerDetectorSpatialLocalizer. For the DSLR filming experience, the compositor window in the editor can be used to generate this alignment request. For non-DSLR filming, a LocalizationInitializer can be added to both devices. Regardless of how localization is started, SpatialLocalizationSettings need to be defined and provided to the MarkerDetectorSpatialLocalizer to create a LocalizationSession.

2. In the created LocalizationSession, a call is made to a MarkerDetectorCoordinateService to start discovering SpatialCoordinates, which kicks off marker detection.

3. Once a marker has been found that has the id provided thorugh the SpatialLocalizationSettings, a SpatialCoordinate is created, completing the LocalizationSession.

4. After SpatialCoordinate locations are found on both devices, they are shared with one another through the SpatialCoordinateSystemParticipant, which allows for the scene to be aligned. 

# Setup
Different spatial alignment strategies require different external dependencies, which will require different setup steps. Be sure to obtain the correct dependencies defined [here](SpectatorView.Setup.md).
