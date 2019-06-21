# Overview

[SpectatorView](Scripts/SpectatorView.cs) is a multi-device experience that allows a user's HoloLens application to be viewed by additional devices at their own viewpoints. It offers functionality for unidirectional content synchronization (State Synchronization) and leverages spatial coordinates for scene alignment (Spatial Alignment). It can be used to enable a variety of filming experiences including documenting prototypes and keynote demos.

## Application Flow

### Pre-compilation
1) The main user's ip address as well as a network port are hardcoded in the application. This ip address allows spectator devices to connect to the user device. Hardcoding ip addresses and port numbers has limitations (The same compiled application cannot currently be used for different user devices). Long term, this matchmaking process will be replaced with a more robust solution.

### In application
1) First, the user's device starts listening for network connections on the specified network port. Spectator devices then connect to the user's device using the user ip address and the same network port. This is facilitated through the [TCPConnectionManager](../Socketer/Scripts/TCPConnectionManager.cs).

2) With each connection, the user application sets up state synchronization and spatial alignment for the spectator application. Both state synchronization and spatial alignment use the same network connection, but they aren't directly related to one another and run in parallel.

### State synchronization
For more information on state synchronization, see [SpectatorView.StateSynchronization](SpectatorView.StateSynchronization.md)

### Spatial alignment
For more information on spatial alignment, see [SpectatorView.SpatialAlignment](SpectatorView.SpatialAlignment.md)

### Screen Recording
For more information on screen recording, see [SpectatorView.ScreenRecording](SpectatorView.ScreenRecording.md)