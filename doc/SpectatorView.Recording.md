# Recording Android and iOS experiences
Spectator View supports filming HoloLens experiences with mobile devices. All mobile recording functionality is enabled through screen capture; however, the associated logic varies across platforms.
- On Android, a custom [ScreenRecorderActivity](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/Plugins/Android/ScreenRecorderActivity.java) has been defined and wrapped by the [AndroidRecordingService](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/AndroidRecordingService.cs).
- On iOS, the [iOSRecordingService](../src/SpectatorView.Unity/Assets/SpectatorView/Scripts/ScreenRecording/iOSRecordingService.cs) has been constructed based on Apple's [ReplayKit](https://developer.apple.com/documentation/replaykit) component.
>Note: On Android, recorded content is always saved to a file; whereas on iOS, recorded content is saved to a temporary file that must be manually saved by the user.

### Application Flow
On start, the SpectatorView MonoBehaviour checks whether recording is enabled through MobileRecordingSetting's "Enable Mobile Recording Service" flag. If said flag is enabled, the declared IRecordingServiceVisual prefab is created in the Unity scene. The IRecordingServiceVisual is then handed a reference to the IRecordingService associated with the current mobile platform. User interactions with the IRecordingServiceVisual can then use this reference to start and stop recording as well as open videos for viewing.

### Custom IRecordingServiceVisuals
SpectatorView supports specifying a custom prefab for starting and stopping recording. To do this, create a prefab containing an Unity component that implements IRecordingServiceVisual. Then, specify this prefab as the 'Override Mobile Recording Service Visual' in the MobileRecordingSettings declared for the scene (To view these settings, press Spectator View -> Edit Settings in the Unity toolbar). When the application starts, the SpectatorView MonoBehaviour will instantiate this prefab. It will then search for an IRecordingServiceVisual in the created prefab to provide a reference to the IRecordingService.

# Recording DSLR experiences

coming soon...
