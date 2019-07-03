# Spatial Alignment

Spatial Alignment component provides abstractions for localization of MR content within the physical world. This also includes abstractions and implementations for the process of exchanging localization information between devices.

> Note: The code is currently split between this folder,  `..\SpatialAlignment.ASA` and `..\SpectatorView\Scripts\SpatialAlignment`; this will be reconciled in the future updates.

## Platform Support

Not all spatial alignment strategies support all platforms. See the chart below to determine which strategy best addresses your intended user scenarios.

| Platform  Support      | HoloLens 2 | HoloLens 1 | Android | iOS |
|:----------------------:|:----------:|:----------:|:-------:|:---:|
| Azure Spatial Anchors  | x          | x          | x       | x   |
| QR Code Detection      | x          |            | x       | x   |
| ArUco Marker Detection |            | x          | x       | x   |

## Key Concepts

Before diving into the abstractions, we operate on two concepts when speaking of localization:

- **Coordinate Space:** When a rotation/position is meant to be relative to a specific coordinate (location in the real world), we say it is in coordinate space. These rotations/positions can be shared across devices in order to define understood rotations/positions in the shared experience.
- **Application World Space:** The rotations/positions that are set in Unity to `.position` and `.rotation` properties of `Transform` objects, are specific to the local application's own "world space". This "world space" is used by the local application to determine how to lay out content relative to each other; and this "world space" itself is relative to the position/rotation of device when the application was launched.

The following constructs compose the abstraction and facilitate the the localization processes:

- **`ISpatialCoordinate`:** The abstract construct symbolizing a physical world coordinate that can be used to convert between application's world space and coordinate-relative space.
- **`ISpatialCoordinateService`:** A service for discovering and managing `ISpatialCoordinates`. Different implementations exists based on a different localization methods.
- **`SpatialLocalizerInitializer`:** The construct that begins and facilitates the creation/sharing of ISpatialCoordinates between the local and remote `SpatialLocalizer`.
  - **`SpatialLocalizer`:** This related construct understands how to localize upon or create a `ISpatialCoordinate` for localization.
- **`SpatialCoordinateSystemManager`:** The singleton manager that manages the incoming/outgoing networking connections, their associated localization state and assigned `ISpatialCoordinates` to them.
- **`ISpatialLocalizationSettings`:** This component exposes the configuration settings for a specific type of SpatialLocalizer.
  - This class is added to a specially generated prefab in the consuming application, see [Spatial Alignment Dependencies](../../../../doc/SpectatorView.Setup.md##%20Spatial%20Localizer%20Dependencies) for detailed configuration instructions.
- **`SpatialCoordinateSystemParticipant`:** Represents the localization state of a connected device, including the location and state of the connected device's shared spatial coordinate.

Furthermore, the following components play a key role in localization:

- **SpectatorView:** This manager singleton selects appropriate mechanism for localization based on current and connected device registration.

## Localization of Devices

The process by which two or more devices agree upon localization details is split into several parts:

- Registration & Configuration
- Selection of Localization Method at Connection Time
- Exchange of Localization Information

### Registration & Configuration

Both of these aspects are required to enable a method to be used for localization, however, they are slightly different. `SpectatorView` comes pre-registered with several `SpatialLocalizers` that can be found on `SpectatorView\Prefabs\SpectatorView.SpatialCoordinateLocalizers.prefab`:

- Azure Spatial Anchors (`SpatialAnchorsLocalizer`) localizer will rely on the hosting (User) device to create the common ISpatialCoordinate to be used by all ASA spectating devices connecting
- Physical Marker localizers will search for some physical marker in the world
- Marker Visual localizer pairs will display a marker on the screen of a mobile device to be discovered by the other device.

Some of these localization methods require settings, which are set through a `SpatialLocalizerInitializer`, two can be found on that prefab for the QR and ArUco visual localizers. Additional settings must be added manually by the consuming application onto `Generated.StateSynchronization.AssetCaches\Resources\SpectatorViewSettings.prefab` which is created by invoking the `Spectator View > Edit Settings` menu item, see [Spatial Alignment Dependencies](../../../../doc/SpectatorView.Setup.md##%20Spatial%20Localizer%20Dependencies).

### Selection of Localization Method

When the application starts and `SpectatorView` is initialized, configured localizers are checked for whether they are supported in the current application on the current device and if they are, they are registered with `SpectatorView`. Afterwards, the process is as follows:

1. `SpatialCoordinateSystemManager` listens for incoming/outgoing network connections, creating a `SpatialCoordinateSystemParticipant` for each connection.
2. `SpectatorView` on the spectating device listens for the creations of these participants, and queries for supported localizers of the participant.
3. Then, based on its own configured prioritized list of `SpatialLocalizationInitializers` and the supported list returned by the hosting (User) device, it identifies the best localization method to use.

> Note: Best localization method is determined as the lowest index of the supported localizers in its configured list.

### Exchange of Localization Information

Having determined the appropriate `SpatialLocalizationInitializer` to use, `SpectatorView` invokes its `RunLocalization` method.

1. The `SpatialLocalizationInitializer` is then responsible for appropriately instantiating and configuring a SpatialLocalizer for localization. It must instantiate and configure localizers for both the local (spectator) and remote (User) participants. Example:
    - For spectator on the mobile phone, a localizer that will display a marker visual is created.
    - For remote participant on the HL2, a localizer will create a localizer that will instruct the remote participant to create appropriate marker detector.
2. Each of instance of the localizers will then execute the appropriate logic to exchange and create the `ISpatialCoordinate` to be used for localization.
3. When the coordinate is created, the participant is updated with it, and other systems (such as `SpatialCoordinateTransformer`) will use it to synchronize positions.

## Detailed Breakdown of Spatial Localization Methods

### Azure Spatial Anchors Localization

Localization here happens by having the hosting device (User) create an `ISpatialCoordinate` backed by an Azure Spatial Anchor, it will then pass this coordinate to every spectating device requesting it.

1. `SpatialAnchorsCoordinateLocalizationInitializer` will configure a SpatialAnchorsLocalizer using the appropriate Azure settings.
2. The hosting (User) device will be instructed to create a localization session, and in turn create (if needed) an Azure Spatial Anchors `ISpatialCoordinate`.
3. The Id of this coordinate will then be communicated to the spectating device.
4. The spectating version of SpatialAnchorsLocalizer will go ahead and run an ASA discovery session until this coordinate is located.
5. Once the discovery session locates the coordinate, both sessions are completed.

### Marker Visuals and Marker Detection (QR Codes and ArUco Markers)

Spatial alignment based on marker visuals and marker detection allows spectator mobile devices to align with a user HoloLens device. Different marker detectors may be used in the experience (QR Code detection is supported for HoloLens 2, while ArUco marker detection is supported for HoloLens 1), but the general application flow is provided below:

1. Using a LocalizationInitializer, the mobile device instructs the user HoloLens device to create a LocalizationSession for a MarkerVisualDetectorSpatialLocalizer. This requires populating and sending SpatialLocalizationSettings.

2. After telling the user HoloLens device to localize, the mobile device creates its own LocalizationSession using a MarkerVisualSpatialLocalizer. This again requires populating SpatialLocalizationSettings.

3. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device tells the user HoloLens device what marker ids are supported by its marker visual.

4. In the MarkerVisualDetectorSpatialLocalizer LocalizationSession, the user HoloLens device assigns the mobile device a marker id. The user HoloLens then begins marker detection by starting coordinate discovery for the MarkerDetectorCoordinateService.

5. In the MarkerVisualSpatialLocalizer LocalizationSession, the mobile device receives its assigned marker id. It then shows a marker visual by starting coordinate discovery for the MarkerVisualCoordinateService.

6. Once the user HoloLens has detected the marker being displayed on the mobile device, a SpatialCoordinate is created and the mobile device is told that the marker visual has been found. The creation of this SpatialCoordinate completes the LocalizationSession on the user HoloLens.

7. Once informed that the marker has been found, the mobile device creates a SpatialCoordinate that reflects the marker visual's location at the time of detection. The creation of this SpatialCoordinate completes the LocalizationSession on the mobile device.

8. The SpatialCoordinate locations found on both devices are then shared with one another, which allows for the scene to be aligned.

### Physical Marker Detection  (QR Codes and ArUco Markers)

Spatial alignment based on physical marker detection allows a spectator HoloLens device to align with a user HoloLens device. Again, different marker detectors may be used in this experience, but the application flow is the following:

1. The SpatialCoordinateSystemManager is told to start localization using a MarkerDetectorSpatialLocalizer. For the DSLR filming experience, the compositor window in the editor can be used to generate this alignment request. For non-DSLR filming, a LocalizationInitializer can be added to both devices. Regardless of how localization is started, SpatialLocalizationSettings need to be defined and provided to the MarkerDetectorSpatialLocalizer to create a LocalizationSession.

2. In the created LocalizationSession, a call is made to a MarkerDetectorCoordinateService to start discovering SpatialCoordinates, which kicks off marker detection.

3. Once a marker has been found that has the id provided through the SpatialLocalizationSettings, a SpatialCoordinate is created, completing the LocalizationSession.

4. After SpatialCoordinate locations are found on both devices, they are shared with one another through the SpatialCoordinateSystemParticipant, which allows for the scene to be aligned.

## Setup

Different spatial alignment strategies require different external dependencies, which will require different setup steps. Be sure to obtain the correct dependencies defined [here](../../../../doc/SpectatorView.Setup.md).
