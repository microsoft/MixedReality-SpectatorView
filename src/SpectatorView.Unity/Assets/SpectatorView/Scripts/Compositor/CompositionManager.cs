// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Manages compositing real-world video and holograms together and creating an output
    /// video texture with recorded audio.
    /// </summary>
    public class CompositionManager : MonoBehaviour
    {
        private const int DSPBufferSize = 1024;
        private const AudioSpeakerMode SpeakerMode = AudioSpeakerMode.Stereo;

        public enum Depth { None, Sixteen = 16, TwentyFour = 24 }
        public enum AntiAliasingSamples { One = 1, Two = 2, Four = 4, Eight = 8 };

        /// <summary>
        /// Gets the texture manager used for compositing.
        /// </summary>
        public TextureManager TextureManager => textureManager;

        /// <summary>
        /// Gets or sets the texture depth used for the RenderTextures used during compositing.
        /// </summary>
        [Header("Hologram Settings")]
        [Tooltip("Texture depth for the RenderTexture used by the compositor")]
        public Depth TextureDepth = Depth.TwentyFour;

        /// <summary>
        /// Gets or sets the sampling level for antialiasing when supersampling is used.
        /// </summary>
        [Tooltip("Anti-aliasing sampling level for downsampling the supersample textures to the target video resolution.")]
        public AntiAliasingSamples AntiAliasing = AntiAliasingSamples.Eight;

        /// <summary>
        /// Gets or sets the filter mode for downsampling when supersampling is used.
        /// </summary>
        [Tooltip("Filtering mode used for downsampling the supersample textures to the target video resolution.")]
        public FilterMode Filter = FilterMode.Trilinear;

        /// <summary>
        /// Gets or sets the number of additional buffers to use for supersampling.
        /// Each additional buffer doubles the size of the rendered holograms before they're
        /// downsampled to the video resolution.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("Number of additional buffers used to render holograms at a higher resolution. Each additional level doubles the size of the rendered hologram before it is downsampled to the video resolution.")]
        public int SuperSampleLevel = 0;

        /// <summary>
        /// Gets or sets the alpha value used for rendering holograms on top of video.
        /// </summary>
        [Tooltip("Default alpha for the holograms in the composite video.")]
        public float DefaultAlpha = 0.9f;

        /// <summary>
        /// Gets or sets whether microphone audio should be recorded into the output video.
        /// </summary>
        [Tooltip("Enables or disables recording microphone audio when recording videos.")]
        public bool EnableMicrophoneAudio = true;

        /// <summary>
        /// Check to enable debug logging.
        /// </summary>
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private bool debugLogging = false;
#pragma warning restore 414

        private float videoTimestampToHolographicTimestampOffset = -10.0f;
        private int captureDeviceIndex = -1;
        private int videoRecordingLayout = -1;
        private TextureManager textureManager = null;
        private MicrophoneInput microphoneInput;

        private bool isVideoFrameProviderInitialized = false;
        private SpectatorViewPoseCache poseCache = new SpectatorViewPoseCache();
        private SpectatorViewTimeSynchronizer timeSynchronizer = new SpectatorViewTimeSynchronizer();

        private Camera spectatorCamera;
        private GameObject videoCameraPose;

        /// <summary>
        /// Gets the index of the video frame currently being composited.
        /// </summary>
        public int CurrentCompositeFrame { get; private set; }

        /// <summary>
        /// Gets whether or not the video frame provider has finished initialization.
        /// </summary>
        public bool IsVideoFrameProviderInitialized => isVideoFrameProviderInitialized;

#if UNITY_EDITOR
        /// <summary>
        /// Gets the calibration data loaded by the camera.
        /// </summary>
        public ICalibrationData CalibrationData => calibrationData;

        /// <summary>
        /// Gets whether or not a holographic camera rig has sent calibration data
        /// that has been loaded to set up the virtual camera pose for rendering.
        /// </summary>
        public bool IsCalibrationDataLoaded { get; private set; }

        private bool overrideCameraPose;
        private Vector3 overrideCameraPosition;
        private Quaternion overrideCameraRotation;
        private MemoryStream audioMemoryStream = null;
        private ICalibrationData calibrationData;

        /// <summary>
        /// Clears the usage of an overridden camera pose and returns to normal pose calculations.
        /// </summary>
        public void ClearOverridePose()
        {
            overrideCameraPose = false;
        }

        /// <summary>
        /// Stops computing the position and rotation of the holographic camera from external sources
        /// and instead fixes the position and rotation as specified.
        /// </summary>
        /// <param name="position">The local position of the holographic camera.</param>
        /// <param name="rotation">The local rotation of the holographic camera.</param>
        public void SetOverridePose(Vector3 position, Quaternion rotation)
        {
            overrideCameraPose = true;
            overrideCameraPosition = position;
            overrideCameraRotation = rotation;
        }
#endif

        /// <summary>
        /// Gets or sets the additional time offset in seconds to adjust holographic timestamps
        /// from the HoloLens to video timestamps from the compositor.
        /// </summary>
        public float VideoTimestampToHolographicTimestampOffset
        {
            get
            {
                if (videoTimestampToHolographicTimestampOffset < -1.0f)
                {
                    videoTimestampToHolographicTimestampOffset = PlayerPrefs.GetFloat(nameof(VideoTimestampToHolographicTimestampOffset));
                }
                return videoTimestampToHolographicTimestampOffset;
            }
            set
            {
                if (videoTimestampToHolographicTimestampOffset != value)
                {
                    videoTimestampToHolographicTimestampOffset = value;
                    PlayerPrefs.SetFloat(nameof(VideoTimestampToHolographicTimestampOffset), videoTimestampToHolographicTimestampOffset);
                    PlayerPrefs.Save();
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of capture device to read video content from.
        /// </summary>
        public FrameProviderDeviceType CaptureDevice
        {
            get
            {
                if (captureDeviceIndex == -1)
                {
                    captureDeviceIndex = PlayerPrefs.GetInt(nameof(CaptureDevice), (int)FrameProviderDeviceType.None);
                }
                return (FrameProviderDeviceType)captureDeviceIndex;
            }
            set
            {
                if (captureDeviceIndex != (int)value)
                {
                    captureDeviceIndex = (int)value;
                    PlayerPrefs.SetInt(nameof(CaptureDevice), captureDeviceIndex);
                    PlayerPrefs.Save();
                }
            }
        }

        public VideoRecordingFrameLayout VideoRecordingLayout
        {
            get
            {
                if (videoRecordingLayout == -1)
                {
                    videoRecordingLayout = PlayerPrefs.GetInt(nameof(VideoRecordingLayout), (int)VideoRecordingFrameLayout.Composite);
                }
                return (VideoRecordingFrameLayout)videoRecordingLayout;
            }
            set
            {
                if (videoRecordingLayout != (int)value)
                {
                    videoRecordingLayout = (int)value;
                    PlayerPrefs.SetInt(nameof(VideoRecordingLayout), videoRecordingLayout);
                    PlayerPrefs.Save();
                }
            }
        }

        #region AudioData
        private BinaryWriter audioStreamWriter;
        private double audioStartTime;
        private int numCachedAudioFrames;
        private const int MAX_NUM_CACHED_AUDIO_FRAMES = 5;
        #endregion

        const int storedStatisticsCapacity = 60;
        Queue<float> framerateStatistics = new Queue<float>(storedStatisticsCapacity);

        public Queue<float> FramerateStatistics => framerateStatistics;

        private void UpdateStatsElement(Queue<float> statElements, float newVal)
        {
            if (statElements.Count == storedStatisticsCapacity)
            {
                statElements.Dequeue();
            }

            statElements.Enqueue(newVal);
        }
       
        private void Start()
        {
            spectatorCamera = GetComponent<Camera>();

#if UNITY_EDITOR
            IsCalibrationDataLoaded = false;
#else
            Camera[] cameras = gameObject.GetComponentsInChildren<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].enabled = false;
            }
#endif
            Initialize();
        }

        private void Initialize()
        {
#if UNITY_EDITOR
            // Ensure that runInBackground is set to true so that the compositor can run even when not focused
            Application.runInBackground = true;

            textureManager = gameObject.AddComponent<TextureManager>();
            microphoneInput = GetComponentInChildren<MicrophoneInput>();
            textureManager.Compositor = this;

            // Change audio listener to the holographic camera.
            AudioListener listener = null;
            var mainCamera = Camera.main;
            if (mainCamera)
            {
                listener = mainCamera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    GameObject.DestroyImmediate(listener);
                }
            }

            listener = GetComponent<AudioListener>();
            if (listener == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            // Disable vsync in editor to ensure that the game runs at the maximum possible framerate
            QualitySettings.vSyncCount = 0;

            AudioConfiguration currentConfiguration = AudioSettings.GetConfiguration();
            currentConfiguration.dspBufferSize = DSPBufferSize;
            currentConfiguration.speakerMode = SpeakerMode;
            AudioSettings.Reset(currentConfiguration);

            // resetting the audio settings kills the mic so don't init until here
            if (EnableMicrophoneAudio && microphoneInput != null)
            {
                microphoneInput.StartMicrophone();
            }
#endif
        }

        /// <summary>
        /// Adds a new camera pose with the time the camera was at when that pose was registered.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera relative to the origin anchor.</param>
        /// <param name="cameraRotation">The rotation of the camera relative to the origin anchor.</param>
        /// <param name="cameraTimestamp">The timestamp the pose was recorded at in the camera's time system.</param>
        public void AddCameraPose(Vector3 cameraPosition, Quaternion cameraRotation, float cameraTimestamp)
        {
            poseCache.AddPose(cameraPosition, cameraRotation, cameraTimestamp);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets the width of the frame for video recording purposes, based
        /// on the current rendering mode.
        /// </summary>
        public int VideoRecordingFrameWidth
        {
            get
            {
                return UnityCompositorInterface.GetVideoRecordingFrameWidth(VideoRecordingLayout);
            }
        }

        /// <summary>
        /// Gets the width of the frame for video recording purposes, based
        /// on the current rendering mode.
        /// </summary>
        public int VideoRecordingFrameHeight
        {
            get
            {
                return UnityCompositorInterface.GetVideoRecordingFrameHeight(VideoRecordingLayout);
            }
        }

        /// <summary>
        /// Gets the time duration of a single video frame.
        /// </summary>
        /// <returns>The time duration of a single video frame, in seconds.</returns>
        private float GetVideoFrameDuration()
        {
            return (0.0001f * UnityCompositorInterface.GetColorDuration() / 1000);
        }

        /// <summary>
        /// Gets the time for a video frame relative to the start of video capture.
        /// </summary>
        /// <param name="frame">The index of the video frame.</param>
        /// <returns>The time of the video frame relative to the start of the video capture, in seconds.</returns>
        private float GetTimeFromFrame(int frame)
        {
            return GetVideoFrameDuration() * frame;
        }

        /// <summary>
        /// Gets the framerate of the input video stream.
        /// </summary>
        /// <returns>The framerate, in frames per second.</returns>
        public float GetVideoFramerate()
        {
            return 1.0f / GetVideoFrameDuration();
        }

        /// <summary>
        /// Gets the width of the video frame.
        /// </summary>
        /// <returns>The width of the video frame, in pixels.</returns>
        public static int GetVideoFrameWidth()
        {
            return UnityCompositorInterface.GetFrameWidth();
        }

        /// <summary>
        /// Gets the height of the video frame.
        /// </summary>
        /// <returns>The height of the video frame, in pixels.</returns>
        public static int GetVideoFrameHeight()
        {
            return UnityCompositorInterface.GetFrameHeight();
        }

        /// <summary>
        /// Gets the number of composited frames ready to be output by the video output buffer.
        /// </summary>
        public int GetQueuedOutputFrameCount()
        {
            return UnityCompositorInterface.GetNumQueuedOutputFrames();
        }

        /// <summary>
        /// Returns true if the UnityCompositor dll supports the specified provider.
        /// </summary>
        /// <param name="providerId">provider to check</param>
        /// <returns>Returns true if the provider is supported, otherwise false.</returns>
        public bool IsFrameProviderSupported(FrameProviderDeviceType providerId)
        {
            return UnityCompositorInterface.IsFrameProviderSupported(providerId);
        }
#endif

        /// <summary>
        /// Gets the timestamp of the hologram that will be composited for the current frame of the compositor.
        /// </summary>
        /// <returns>The hologram timestamp corresponding to the current video frame, in Unity's timeline.</returns>
        public float GetHologramTime()
        {
            float time = Time.time;

#if UNITY_EDITOR
            if (isVideoFrameProviderInitialized)
            {
                if (poseCache.poses.Count > 0)
                {
                    time = timeSynchronizer.GetUnityTimeFromCameraTime(GetTimeFromFrame(CurrentCompositeFrame));
                }
                else
                {
                    //Clamp time to video dt
                    float videoDeltaTime = GetVideoFrameDuration();
                    int frame = (int)(time / videoDeltaTime);
                    //Subtract the queued frames
                    frame -= UnityCompositorInterface.GetCaptureFrameIndex() - CurrentCompositeFrame;
                    time = videoDeltaTime * frame;
                }
            }
#endif

            return time;
        }

        private void Update()
        {
#if UNITY_EDITOR

            UpdateStatsElement(framerateStatistics, 1.0f / Time.deltaTime);

            int captureFrameIndex = UnityCompositorInterface.GetCaptureFrameIndex();

            int prevCompositeFrame = CurrentCompositeFrame;

            //Set our current frame towards the latest captured frame. Dont get too close to it, and dont fall too far behind 
            int step = (captureFrameIndex - CurrentCompositeFrame);
            if (step < 8)
            {
                step = 0;
            }
            else if (step > 16)
            {
                step -= 16;
            }
            else
            {
                step = 1;
            }
            CurrentCompositeFrame += step;

            UnityCompositorInterface.SetCompositeFrameIndex(CurrentCompositeFrame);

            #region Spectator View Transform
            if (IsCalibrationDataLoaded && transform.parent != null)
            {
                //Update time syncronizer
                {
                    float captureTime = GetTimeFromFrame(captureFrameIndex);

                    SpectatorViewPoseCache.PoseData poseData = poseCache.GetLatestPose();
                    if (poseData != null)
                    {
                        timeSynchronizer.Update(UnityCompositorInterface.GetCaptureFrameIndex(), captureTime, poseData.Index, poseData.TimeStamp);
                    }
                }

                if (overrideCameraPose)
                {
                    transform.parent.localPosition = overrideCameraPosition;
                    transform.parent.localRotation = overrideCameraRotation;
                }
                else
                {
                    //Set camera transform for the currently composited frame
                    float cameraTime = GetTimeFromFrame(prevCompositeFrame);
                    float poseTime = timeSynchronizer.GetPoseTimeFromCameraTime(cameraTime);

                    Quaternion currRot;
                    Vector3 currPos;
                    poseTime += VideoTimestampToHolographicTimestampOffset;
                    if (captureFrameIndex <= 0) //No frames captured yet, lets use the very latest camera transform
                    {
                        poseTime = float.MaxValue;
                    }
                    poseCache.GetPose(poseTime, out currPos, out currRot);

                    transform.parent.localPosition = currPos;
                    transform.parent.localRotation = currRot;
                }
            }

            #endregion

            if (!isVideoFrameProviderInitialized)
            {
                if (UnityCompositorInterface.IsFrameProviderSupported(CaptureDevice))
                {
                    isVideoFrameProviderInitialized = UnityCompositorInterface.InitializeFrameProviderOnDevice(CaptureDevice);
                    if (isVideoFrameProviderInitialized)
                    {
                        CurrentCompositeFrame = 0;
                        timeSynchronizer.Reset();
                        poseCache.Reset();
                    }
                }
                else
                {
                    Debug.LogWarning($"The current capture device, {CaptureDevice}, is not supported by your build of SpectatorView.Compositor.UnityPlugin.dll.");
                }
            }

            UnityCompositorInterface.UpdateCompositor();
#endif
        }

        private void UpdateCullingMask()
        {
            // Copy the culling mask from the main camera to the spectator camera
            Camera cam = Camera.main;
            if (cam)
            {
                spectatorCamera.cullingMask = cam.cullingMask;
            }
        }

        protected void OnPreCull()
        {
            UpdateCullingMask();
        }

        /// <summary>
        /// Enables the holographic camera rig for compositing. The hologram camera will be adjusted to match
        /// calibration data (its position and rotation will track the external camera, and its projection matrix
        /// will match the calibration information for the video camera used for compositing).
        /// </summary>
        /// <param name="parent">The parent transform that the holographic camera rig should be attached to.</param>
        /// <param name="calibrationData">The calibration data used to set up the position, rotation, and
        /// projection matrix for the holographic camera.</param>
        public void EnableHolographicCamera(Transform parent, ICalibrationData calibrationData)
        {
#if UNITY_EDITOR
            this.calibrationData = calibrationData;
            if (videoCameraPose == null)
            {
                videoCameraPose = new GameObject("Camera HMD Pose");
            }

            videoCameraPose.transform.SetParent(parent);
            videoCameraPose.transform.localPosition = Vector3.zero;
            videoCameraPose.transform.localRotation = Quaternion.identity;

            gameObject.transform.parent = videoCameraPose.transform;

            calibrationData.SetUnityCameraExtrinstics(transform);
            calibrationData.SetUnityCameraIntrinsics(GetComponent<Camera>());

            IsCalibrationDataLoaded = true;
#endif
        }

        /// <summary>
        /// Clears cached information about synchronized poses and time offsets.
        /// </summary>
        public void ResetOnNewCameraConnection()
        {
#if UNITY_EDITOR
            IsCalibrationDataLoaded = false;
#endif
            timeSynchronizer.Reset();
            poseCache.Reset();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            isVideoFrameProviderInitialized = false;
        }

        private void OnDestroy()
        {
            ResetCompositor();
        }

        private void ResetCompositor()
        {
            DebugLog("Stopping the video composition system.");
            UnityCompositorInterface.Reset();

            UnityCompositorInterface.StopFrameProvider();
            if (UnityCompositorInterface.IsRecording())
            {
                UnityCompositorInterface.StopRecording();
            }
        }

        // This function is not/not always called on the main thread.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!UnityCompositorInterface.IsRecording())
            {
                return;
            }

            //Create new stream
            if (audioMemoryStream == null)
            {
                audioMemoryStream = new MemoryStream();
                audioStreamWriter = new BinaryWriter(audioMemoryStream);
                double audioSettingsTime = AudioSettings.dspTime; // Audio time in seconds, more accurate than Time.time
                double captureFrameTime = UnityCompositorInterface.GetCaptureFrameIndex() * UnityCompositorInterface.GetColorDuration() / 10000000.0; // Capture Frame Time in seconds
                DebugLog($"Obtained Audio Sample, AudioSettingsTime:{audioSettingsTime}, CaptureFrameTime:{captureFrameTime}");
                audioStartTime = captureFrameTime;
                numCachedAudioFrames = 0;
            }

            //Put data into stream
            for (int i = 0; i < data.Length; i++)
            {
                // Rescale float to short range for encoding.
                short audioEntry = (short)(data[i] * short.MaxValue);
                audioStreamWriter.Write(audioEntry);
            }

            numCachedAudioFrames++;

            //Send to compositor (buffer a few calls to reduce potential timing errors between packages)
            if (numCachedAudioFrames >= MAX_NUM_CACHED_AUDIO_FRAMES)
            {
                audioStreamWriter.Flush();
                byte[] outBytes = audioMemoryStream.ToArray();
                audioMemoryStream = null;

                // The Unity compositor assumes that the audioStartTime will be in capture frame sample time.
                // Above we default to capture frame time compared to AudioSettings.dspTime.
                // Any interpolation between these two time sources needs to be done in the editor before handing sample time values to the compositor.
                UnityCompositorInterface.SetAudioData(outBytes, outBytes.Length, audioStartTime);
            }
        }

        public void TakePicture()
        {
            UnityCompositorInterface.TakePicture();
        }

        public bool IsRecording()
        {
            return UnityCompositorInterface.IsRecording();
        }

        public bool TryStartRecording(out string fileName)
        {
            fileName = string.Empty;
            TextureManager.InitializeVideoRecordingTextures();
            StringBuilder builder = new StringBuilder(1024);
            string documentDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string outputDirectory = $"{documentDirectory}\\HologramCapture";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string desiredFileName = $"{outputDirectory}\\Video.mp4";
            int[] fileNameLength = new int[1];
            bool startedRecording = UnityCompositorInterface.StartRecording((int)VideoRecordingLayout, desiredFileName, desiredFileName.Length, builder.Capacity, builder, fileNameLength);
            if (!startedRecording)
            {
                Debug.LogError($"CompositionManager failed to start recording: {desiredFileName}");
                return false;
            }

            fileName = builder.ToString().Substring(0, fileNameLength[0]);
            DebugLog($"Started recording file: {fileName}");
            return true;
        }

        public void StopRecording()
        {
            StopRecordingAudio();
            UnityCompositorInterface.StopRecording();
        }

        /// <summary>
        /// Stops audio recording by ensuring the audio stream is fully written immediately.
        /// </summary>
        private void StopRecordingAudio()
        {
            //Send any left over stream
            if (audioMemoryStream != null)
            {
                audioStreamWriter.Flush();
                byte[] outBytes = audioMemoryStream.ToArray();
                UnityCompositorInterface.SetAudioData(outBytes, outBytes.Length, audioStartTime);
                audioMemoryStream = null;
            }
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"CompositionManager: {message}");
            }
        }
#endif
    }
}