# Recording Android and iOS experiences

Spectator View supports filming HoloLens experiences with mobile devices. All mobile recording functionality is enabled through screen capture; however, the associated logic varies across platforms.

- On Android, a custom [ScreenRecorderActivity](Plugins/Android/ScreenRecorderActivity.java) has been defined and wrapped by the [AndroidRecordingService](AndroidRecordingService.cs).
- On iOS, the [iOSRecordingService](iOSRecordingService.cs) has been constructed based on Apple's [ReplayKit](https://developer.apple.com/documentation/replaykit) component.

>Note: On Android, recorded content is always saved to a file; whereas on iOS, recorded content is saved to a temporary file that must be manually saved by the user.

## Application Flow

On start, the [SpectatorView](../SpectatorView.cs) MonoBehaviour checks whether recording is enabled through [MobileRecordingSetting](MobileRecordingSettings.cs)'s "Enable Mobile Recording Service" flag. If said flag is enabled, the declared [IRecordingServiceVisual](IRecordingServiceVisual.cs) prefab is created in the Unity scene. The [IRecordingServiceVisual](IRecordingServiceVisual.cs) is then handed a reference to the [IRecordingService](IRecordingService.cs) associated with the current mobile platform. User interactions with the [IRecordingServiceVisual](IRecordingServiceVisual.cs) can then use this reference to start and stop recording as well as open videos for viewing.

## Custom IRecordingServiceVisuals

The recording service visual shown on mobile devices can be replaced by your own UI. For more information on how to change out the default Spectator View recording UI with your own custom content, see [here](../UI/README.md).

## Troubleshooting
### iOS screen recording fails to create a video
It happens infrequently, but ReplayKit can return true when attempting to start a recording even though it has failed. One known workaround is to restart your iOS devices and try again.

To test whether or not your device is in this state, use the built in iOS screen recording functionality. Enable screen recording in the control center through Settings -> Control Center -> Customize Controls -> Press the '+' next to Screen Recording. You can then start screen recording through the control center. If screen recording generates an error prompt stating that "Screen recording stopped due to mediaservices failure", you have hit this failure state.
