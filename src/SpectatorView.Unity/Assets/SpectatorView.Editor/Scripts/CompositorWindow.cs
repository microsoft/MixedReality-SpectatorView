// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    [Description("Compositor")]
    internal class CompositorWindow : CompositorWindowBase<CompositorWindow>
    {
        private const float maxFrameOffset = 0.2f;
        private const float statisticsUpdateCooldownTimeSeconds = 0.1f;
        private const int lowQueuedOutputFrameWarningMark = 6;
        private Vector2 scrollPosition;
        private PreviewTextureMode previewTextureMode;
        private string framerateStatisticsMessage;
        private Color framerateStatisticsColor = Color.green;

        private bool compositorStatsFoldout;
        private bool recordingFoldout;
        private bool colorCorrectionFoldout;
        private bool hologramSettingsFoldout;
        private bool occlusionSettingsFoldout;
        private bool devicePortalFoldout;

        private float hologramAlpha;

        private float statisticsUpdateTimeSeconds = 0.0f;
        private string appIPAddress;
        private string devicePortalUser;
        private string devicePortalPassword = "";

        private bool? isAzureKinectBodyTrackingSdkInstalledInUnity;
        private static readonly string[] azureKinectBodyTrackingSdkComponents = new[] { "onnxruntime.dll", "dnn_model_2_0.onnx", "cudnn64_7.dll", "cublas64_100.dll", "cudart64_100.dll" };

        private static string holographicCameraIPAddressKey = $"{nameof(CompositorWindow)}.{nameof(holographicCameraIPAddress)}";
        private static string appIPAddressKey = $"{nameof(CompositorWindow)}.{nameof(appIPAddress)}";
        private static string devicePortalUserKey = $"{nameof(CompositorWindow)}.{nameof(devicePortalUser)}";

        [MenuItem("Spectator View/Compositor", false, 0)]
        public static void ShowCompositorWindow()
        {
            ShowWindow();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            CompositionManager compositionManager = GetCompositionManager();
            if (compositionManager != null)
            {
                hologramAlpha = compositionManager.DefaultAlpha;
            }

            holographicCameraIPAddress = PlayerPrefs.GetString(holographicCameraIPAddressKey, "localhost");
            appIPAddress = PlayerPrefs.GetString(appIPAddressKey, "localhost");
            devicePortalUser = PlayerPrefs.GetString(devicePortalUserKey);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PlayerPrefs.SetString(holographicCameraIPAddressKey, holographicCameraIPAddress);
            PlayerPrefs.SetString(appIPAddressKey, appIPAddress);
            PlayerPrefs.SetString(devicePortalUserKey, devicePortalUser);
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                NetworkConnectionGUI();
                CompositeGUI();
                OcclusionSettingsGUI();
                RecordingGUI();
                ColorCorrectionGUI();
                HologramSettingsGUI();
                CompositorStatsGUI();
                DevicePortalGUI();
            }
            EditorGUILayout.EndScrollView();
        }

        private void NetworkConnectionGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                DeviceInfoObserver stateSynchronizationDevice = null;
                if (StateSynchronizationObserver.IsInitialized)
                {
                    stateSynchronizationDevice = StateSynchronizationObserver.Instance.GetComponent<DeviceInfoObserver>();
                }
                DeviceInfoObserver holographicCameraDevice = GetHolographicCameraDevice();

                HolographicCameraNetworkConnectionGUI(
                    AppDeviceTypeLabel,
                    stateSynchronizationDevice,
                    GetSpatialCoordinateSystemParticipant(stateSynchronizationDevice),
                    showCalibrationStatus: false,
                    showSpatialLocalization: true,
                    ref appIPAddress);
                HolographicCameraNetworkConnectionGUI(
                    HolographicCameraDeviceTypeLabel,
                    holographicCameraDevice,
                    GetSpatialCoordinateSystemParticipant(holographicCameraDevice),
                    showCalibrationStatus: true,
                    showSpatialLocalization: true,
                    ref holographicCameraIPAddress);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CompositeGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            {
                //Title
                CompositionManager compositionManager = GetCompositionManager();
                {
                    string title;
                    if (compositionManager != null && compositionManager.IsVideoFrameProviderInitialized)
                    {
                        float framesPerSecond = compositionManager.GetVideoFramerate();
                        title = string.Format("Composite [{0} x {1} @ {2:F2} frames/sec]", renderFrameWidth, renderFrameHeight, framesPerSecond);
                    }
                    else
                    {
                        title = "Composite";
                    }

                    RenderTitle(title, Color.green);
                }

                EditorGUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (compositionManager != null)
                        {
                            GUI.enabled = compositionManager == null || !compositionManager.IsVideoFrameProviderInitialized;
                            GUIContent captureLabel = new GUIContent("Video source", "The video capture card you want to use as input for compositing.");
                            GUIContent outputLabel = new GUIContent("Video output", "The video capture card you want to use as output after compositing.");

                            var supportedCaptureDevices = Enum.GetValues(typeof(FrameProviderDeviceType))
                                .Cast<FrameProviderDeviceType>()
                                .Where(provider => compositionManager.IsFrameProviderSupported(provider) || (provider == FrameProviderDeviceType.None)).ToList();
                            var selectedIndex = supportedCaptureDevices.IndexOf(compositionManager.CaptureDevice);
                            if (selectedIndex < 0)
                            {
                                selectedIndex = supportedCaptureDevices.Count - 1;
                            }

                            selectedIndex = EditorGUILayout.Popup(captureLabel, selectedIndex,
                                supportedCaptureDevices
                                .Select(x => x.ToString())
                                .ToArray());

                            compositionManager.CaptureDevice = supportedCaptureDevices[selectedIndex];

                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginHorizontal();

                            var supportedOutputDevices = Enum.GetValues(typeof(FrameProviderDeviceType))
                                .Cast<FrameProviderDeviceType>()
                                .Where(provider => compositionManager.IsOutputFrameProviderSupported(provider) || (provider == FrameProviderDeviceType.None)).ToList();
                            selectedIndex = supportedOutputDevices.IndexOf(compositionManager.OutputDevice);
                            if (selectedIndex < 0)
                            {
                                selectedIndex = supportedOutputDevices.Count - 1;
                            }

                            selectedIndex = EditorGUILayout.Popup(outputLabel, selectedIndex,
                                supportedOutputDevices
                                .Select(x => x.ToString())
                                .ToArray());

                            compositionManager.OutputDevice = supportedOutputDevices[selectedIndex];

                            if ((supportedCaptureDevices[selectedIndex] != FrameProviderDeviceType.AzureKinect_DepthCamera_NFOV && supportedCaptureDevices[selectedIndex] != FrameProviderDeviceType.AzureKinect_DepthCamera_WFOV) || compositionManager.VideoRecordingLayout == VideoRecordingFrameLayout.Quad)
                            {
                                GUI.enabled = false;
                            }

                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginHorizontal();

                            GUIContent occlusionLabel = new GUIContent("Occlusion Mode", "The occlusion mode used to determine if real-world or holographic content is displayed using depth information from supported cameras.");

                            var occlusionModes = Enum.GetValues(typeof(OcclusionSetting))
                                .Cast<OcclusionSetting>()
                                .Where(setting => compositionManager.IsOcclusionSettingSupported(setting))
                                .ToList();

                            if (occlusionModes.Count > 0)
                            {
                                selectedIndex = occlusionModes.IndexOf(compositionManager.OcclusionMode);

                                if (selectedIndex < 0)
                                {
                                    selectedIndex = 0;
                                }

                                selectedIndex = EditorGUILayout.Popup(occlusionLabel, selectedIndex, occlusionModes
                                    .Select(x => x.ToString())
                                    .ToArray());

                                compositionManager.OcclusionMode = occlusionModes[selectedIndex];
                            }

                            GUI.enabled = true;

                            if (IsAzureKinectFrameProvider(compositionManager.CaptureDevice) && compositionManager.OcclusionMode == OcclusionSetting.BodyTracking && !IsAzureKinectBodyTrackingSDKInstalledInUnity())
                            {
                                var previousColor = GUI.backgroundColor;
                                GUI.backgroundColor = Color.red;
                                if (GUILayout.Button(new GUIContent("Install Required Components", "Copies components needed for the Azure Kinect Body Tracking into your Unity installation directory (requires elevation)"), GUILayout.Width(250.0f)))
                                {
                                    InstallAzureKinectBodyTrackingComponents();
                                }
                                GUI.backgroundColor = previousColor;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    {
                        string[] compositionOptions = new string[] { "Final composite", "Intermediate textures", "Occlusion Mask" };
                        GUIContent renderingModeLabel = new GUIContent("Preview display", "Choose between displaying the composited video texture, seeing intermediate textures displayed in 4 sections (bottom left: input video, top left: opaque hologram, top right: hologram alpha mask, bottom right: hologram alpha-blended onto video), or viewing the occlusion mask.");
                        previewTextureMode = (PreviewTextureMode)EditorGUILayout.Popup(renderingModeLabel, (int)previewTextureMode, compositionOptions);
                        if (compositionManager != null && compositionManager.TextureManager != null)
                        {
                            // Make sure the textures required for quadrant viewing are created if needed.
                            compositionManager.TextureManager.IsQuadrantVideoFrameNeededForPreviewing = (previewTextureMode == PreviewTextureMode.Quad);
                        }
                        FullScreenCompositorWindow fullscreenWindow = FullScreenCompositorWindow.TryGetWindow();
                        if (fullscreenWindow != null)
                        {
                            fullscreenWindow.PreviewTextureMode = previewTextureMode;
                        }

                        if (GUILayout.Button("Fullscreen", GUILayout.Width(120)))
                        {
                            FullScreenCompositorWindow.ShowFullscreen();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndVertical();

                // Rendering
                CompositeTextureGUI(previewTextureMode);
            }
            EditorGUILayout.EndVertical();
        }

        private void RecordingGUI()
        {
            recordingFoldout = EditorGUILayout.Foldout(recordingFoldout, "Recording");
            if (recordingFoldout)
            {
                CompositionManager compositionManager = GetCompositionManager();

                EditorGUILayout.BeginVertical("Box");
                {
                    RenderTitle("Recording", Color.green);

                    EditorGUILayout.BeginHorizontal("Box");
                    {
                        bool wasEnabled = GUI.enabled;
                        GUI.enabled = compositionManager != null && !compositionManager.IsRecording();
                        string[] compositionOptions = new string[] { "Normal", "Split channels" };
                        GUIContent renderingModeLabel = new GUIContent("Video output mode", "Choose between recording the composited video texture or recording intermediate textures displayed in 4 sections (bottom left: input video, top left: opaque hologram, top right: hologram alpha mask, bottom right: hologram alpha-blended onto video)");
                        int layout = 0;
                        if (compositionManager != null)
                        {
                            layout = (int)compositionManager.VideoRecordingLayout;
                        }

                        layout = EditorGUILayout.Popup(renderingModeLabel, layout, compositionOptions);
                        if (compositionManager != null)
                        {
                            compositionManager.VideoRecordingLayout = (VideoRecordingFrameLayout)layout;
                        }
                        GUI.enabled = wasEnabled;
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.enabled = compositionManager != null && compositionManager.TextureManager != null;
                    if (compositionManager == null || !compositionManager.IsRecording())
                    {
                        if (GUILayout.Button("Start Recording"))
                        {
                            compositionManager.TryStartRecording(out var fileName);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Stop Recording"))
                        {
                            compositionManager.StopRecording();
                        }
                    }

                    if (GUILayout.Button("Take Picture"))
                    {
                        compositionManager.TakePicture();
                    }

                    EditorGUILayout.Space();
                    GUI.enabled = true;

                    // Open Folder
                    if (GUILayout.Button("Open Folder"))
                    {
                        Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HologramCapture"));
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void OcclusionSettingsGUI()
        {
            occlusionSettingsFoldout = EditorGUILayout.Foldout(occlusionSettingsFoldout, "Occlusion Settings");
            if (occlusionSettingsFoldout)
            {
                CompositionManager compositionManager = GetCompositionManager();
                bool running = compositionManager != null && compositionManager.TextureManager != null;
                if (running)
                {
                    if (!compositionManager.IsOcclusionSettingSupported(compositionManager.OcclusionMode))
                    {
                        RenderTitle("The current camera does not support occlusion.", Color.clear);
                    }
                    else if (compositionManager.OcclusionMode == OcclusionSetting.BodyTracking)
                    {
                        RenderTitle("Body Tracking Settings", Color.clear);
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Blur Size");
                            compositionManager.TextureManager.blurSize = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.blurSize, 0, 10);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Number of Blur Passes");
                            compositionManager.TextureManager.numBlurPasses = (int)EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.numBlurPasses, 1, 5);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else if (compositionManager.OcclusionMode == OcclusionSetting.RawDepthCamera)
                    {
                        RenderTitle("Raw Depth Camera Settings", Color.clear);
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Blur Size");
                            compositionManager.TextureManager.blurSize = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.blurSize, 0, 10);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Number of Blur Passes");
                            compositionManager.TextureManager.numBlurPasses = (int)EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.numBlurPasses, 1, 5);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    RenderTitle("Updating occlusion settings is not possible when the compositor isn't running.", Color.green);
                }
                GUI.enabled = true;
            }
        }

        private void ColorCorrectionGUI()
        {
            colorCorrectionFoldout = EditorGUILayout.Foldout(colorCorrectionFoldout, "Color Correction Settings");
            if (colorCorrectionFoldout)
            {
                RenderTitle("Video Camera Color Correction", Color.clear);
                CompositionManager compositionManager = GetCompositionManager();
                bool running = compositionManager != null && compositionManager.TextureManager != null && compositionManager.TextureManager.videoFeedColorCorrection != null;
                if (running)
                {
                    EditorGUILayout.BeginVertical("Box");
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Enabled");
                            compositionManager.TextureManager.videoFeedColorCorrection.Enabled = EditorGUILayout.Toggle(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.Enabled);
                        }
                        EditorGUILayout.EndHorizontal();
                        if (GUI.enabled &&
                            !compositionManager.TextureManager.videoFeedColorCorrection.Enabled)
                        {
                            GUI.enabled = false;
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("R Scale");
                            compositionManager.TextureManager.videoFeedColorCorrection.RScale = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.RScale, 0, 4);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("G Scale");
                            compositionManager.TextureManager.videoFeedColorCorrection.GScale = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.GScale, 0, 4);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("B Scale");
                            compositionManager.TextureManager.videoFeedColorCorrection.BScale = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.BScale, 0, 4);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("H Offset");
                            compositionManager.TextureManager.videoFeedColorCorrection.HOffset = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.HOffset, -1, 1);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("S Offset");
                            compositionManager.TextureManager.videoFeedColorCorrection.SOffset = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.SOffset, -1, 1);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("V Offset");
                            compositionManager.TextureManager.videoFeedColorCorrection.VOffset = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.VOffset, -1, 1);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Brightness");
                            compositionManager.TextureManager.videoFeedColorCorrection.Brightness = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.Brightness, -1, 1);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Contrast");
                            compositionManager.TextureManager.videoFeedColorCorrection.Contrast = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.Contrast, 0, 2);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUIContent label = new GUIContent("Gamma");
                            compositionManager.TextureManager.videoFeedColorCorrection.Gamma = EditorGUILayout.Slider(
                                label,
                                compositionManager.TextureManager.videoFeedColorCorrection.Gamma, 0.1f, 4);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    RenderTitle("Updating color correction is not possible when the compositor isn't running.", Color.green);
                }
                GUI.enabled = true;
            }
        }

        private void HologramSettingsGUI()
        {
            hologramSettingsFoldout = EditorGUILayout.Foldout(hologramSettingsFoldout, "Hologram Settings");
            if (hologramSettingsFoldout)
            {

                EditorGUILayout.BeginVertical("Box");
                {
                    CompositionManager compositionManager = GetCompositionManager();

                    GUIContent alphaLabel = new GUIContent("Alpha", "The alpha value used to blend holographic content with video content. 0 will result in completely transparent holograms, 1 in completely opaque holograms.");
                    float newAlpha = EditorGUILayout.Slider(alphaLabel, this.hologramAlpha, 0, 1);
                    if (newAlpha != hologramAlpha)
                    {
                        hologramAlpha = newAlpha;
                        if (compositionManager != null && compositionManager.TextureManager != null)
                        {
                            compositionManager.TextureManager.SetHologramShaderAlpha(newAlpha);
                        }
                    }

                    EditorGUILayout.Space();

                    if (compositionManager != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            float previousFrameOffset = compositionManager.VideoTimestampToHolographicTimestampOffset;
                            GUIContent frameTimeAdjustmentLabel = new GUIContent("Frame time adjustment", "The time in seconds to offset video timestamps from holographic timestamps. Use this to manually adjust for network latency if holograms appear to lag behind or follow ahead of the video content as you move the camera.");
                            compositionManager.VideoTimestampToHolographicTimestampOffset = EditorGUILayout.Slider(frameTimeAdjustmentLabel, previousFrameOffset, -1 * maxFrameOffset, maxFrameOffset);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void CompositorStatsGUI()
        {
            compositorStatsFoldout = EditorGUILayout.Foldout(compositorStatsFoldout, "Compositor Stats");
            if (compositorStatsFoldout)
            {
                CompositionManager compositionManager = GetCompositionManager();
                if (compositionManager != null && compositionManager.IsVideoFrameProviderInitialized)
                {
                    UpdateStatistics(compositionManager);

                    RenderTitle(framerateStatisticsMessage, framerateStatisticsColor);

                    int queuedFrameCount = compositionManager.GetQueuedOutputFrameCount();
                    Color queuedFrameColor = (queuedFrameCount > lowQueuedOutputFrameWarningMark) ? Color.green : Color.red;
                    RenderTitle($"{queuedFrameCount} Queued output frames", queuedFrameColor);
                }
                else
                {
                    framerateStatisticsMessage = null;
                    framerateStatisticsColor = Color.green;
                    statisticsUpdateTimeSeconds = 0.0f;

                    RenderTitle("Compositor is not running, no statistics available", Color.green);
                }
            }
        }

        private void DevicePortalGUI()
        {
            DevicePortalConnection devicePortal = GetDevicePortal();
            if (devicePortal == null)
            {
                return;
            }

            devicePortalFoldout = EditorGUILayout.Foldout(devicePortalFoldout, "Device Portal");
            if (devicePortalFoldout)
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    bool prevEnabled = GUI.enabled;
                    GUILayout.Label("DevicePortal", EditorStyles.boldLabel);

                    string stateText, buttonText;
                    bool canPressButton = true;
                    Action onPressButton = () => { };
                    switch (devicePortal.State)
                    {
                        case DevicePortalState.NotConnected:
                            stateText = "<color=silver>Not Connected</color>";
                            canPressButton =
                                !string.IsNullOrWhiteSpace(devicePortalUser) &&
                                !string.IsNullOrEmpty(devicePortalPassword) &&
                                IPAddress.TryParse(holographicCameraIPAddress, out var _) &&
                                EditorApplication.isPlaying;
                            buttonText = "Connect";
                            onPressButton = () => devicePortal.StartConnect(holographicCameraIPAddress, devicePortalUser, devicePortalPassword);
                            break;

                        case DevicePortalState.Connecting:
                            stateText = "<color=silver>Connecting...</color>";
                            buttonText = "Cancel";
                            onPressButton = devicePortal.CancelConnect;
                            break;

                        case DevicePortalState.NotStarted:
                            stateText = "<color=red>App was not started</color>";
                            canPressButton = devicePortal.IsAppInstalled;
                            buttonText = "Start App";
                            onPressButton = devicePortal.StartApp;
                            break;

                        case DevicePortalState.NotRunning:
                            stateText = "<color=orange>Started, but not running</color>";
                            buttonText = "Stop App";
                            onPressButton = devicePortal.StopApp;
                            break;

                        case DevicePortalState.Running:
                            stateText = "<color=green>Running</color>";
                            buttonText = "Stop App";
                            onPressButton = devicePortal.StopApp;
                            break;

                        case DevicePortalState.Starting:
                            stateText = "<color=orange>Starting...</color>";
                            buttonText = "Cancel";
                            canPressButton = false;
                            break;

                        case DevicePortalState.Stopping:
                            stateText = "<color=orange>Stopping...</color>";
                            canPressButton = false;
                            buttonText = "Cancel";
                            break;

                        default:
                            stateText = "<color=magenta>I don't know</color>";
                            canPressButton = true;
                            buttonText = "Disconnect";
                            onPressButton = devicePortal.Disconnect;
                            break;
                    }
                    GUIStyle appStateStyle = EditorStyles.textField;
                    appStateStyle.richText = true;
                    appStateStyle.fontStyle = FontStyle.Bold;
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.enabled = false;
                        EditorGUILayout.TextField("State", stateText, appStateStyle);
                        GUI.enabled = prevEnabled && canPressButton;
                        if (GUILayout.Button(buttonText, GUILayout.Width(connectAndDisconnectButtonWidth)))
                        {
                            onPressButton();
                        }
                    }

                    GUI.enabled = false;
                    EditorGUILayout.TextField("IP address", holographicCameraIPAddress);

                    GUI.enabled = prevEnabled && devicePortal.State == DevicePortalState.NotConnected;
                    devicePortalUser = EditorGUILayout.TextField("Username", devicePortalUser);
                    devicePortalPassword = EditorGUILayout.PasswordField("Password", devicePortalPassword);
                    EditorGUILayout.Space();

                    GUI.enabled = prevEnabled && devicePortal.IsConnected;
                    GUILayout.Label("Health", EditorStyles.boldLabel);
                    EditorGUILayout.FloatField(new GUIContent("CPU", "The amount of CPU used by the app"), devicePortal.CPUUsage);
                    EditorGUILayout.TextField(new GUIContent("RAM", "The amount of RAM used by the app"), $"{devicePortal.WorkingSet / 1024 / 1024}MiB");
                    EditorGUILayout.TextField("Battery", $"{(int)(devicePortal.BatteryLevel * 100)}% {(devicePortal.IsPowered ? "- Powered" : "")}");

                    GUI.enabled = prevEnabled;
                }
            }
        }

        private void UpdateStatistics(CompositionManager compositionManager)
        {
            statisticsUpdateTimeSeconds -= Time.deltaTime;
            if (statisticsUpdateTimeSeconds <= 0)
            {
                statisticsUpdateTimeSeconds = statisticsUpdateCooldownTimeSeconds;

                float average;
                framerateStatisticsMessage = GetFramerateStatistics(compositionManager, out average);
                framerateStatisticsColor = (average > compositionManager.GetVideoFramerate()) ? Color.green : Color.red;
            }
        }

        private string GetStatsString(string title, Queue<float> statElements, out float average)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            float total = 0.0f;
            foreach (var v in statElements)
            {
                min = Mathf.Min(v, min);
                max = Mathf.Max(v, max);
                total += v;
            }

            average = total / statElements.Count;
            return string.Format("{0}: Min:{1} Max:{2} Avg:{3:N1}", title, (int)min, (int)max, average);
        }

        private string GetFramerateStatistics(CompositionManager compositionManager, out float average)
        {
            return GetStatsString("Compositor framerate", compositionManager.FramerateStatistics, out average);
        }

        private static bool IsAzureKinectFrameProvider(FrameProviderDeviceType captureDevice)
        {
            return captureDevice == FrameProviderDeviceType.AzureKinect_DepthCamera_Off ||
                captureDevice == FrameProviderDeviceType.AzureKinect_DepthCamera_NFOV ||
                captureDevice == FrameProviderDeviceType.AzureKinect_DepthCamera_WFOV;
        }

        private bool IsAzureKinectBodyTrackingSDKInstalledInUnity()
        {
            if (isAzureKinectBodyTrackingSdkInstalledInUnity == null)
            {
                var unityInstallDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var componentPaths = azureKinectBodyTrackingSdkComponents.Select(component => Path.Combine(unityInstallDirectory, component));
                isAzureKinectBodyTrackingSdkInstalledInUnity = componentPaths.All(path => File.Exists(path));
            }

            return isAzureKinectBodyTrackingSdkInstalledInUnity.Value;
        }

        private void InstallAzureKinectBodyTrackingComponents()
        {
            string componentSourceDirectory = null;

            var assets = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(azureKinectBodyTrackingSdkComponents[0]));
            if (assets.Length == 1)
            {
                componentSourceDirectory = Path.GetDirectoryName(Path.GetFullPath(AssetDatabase.GUIDToAssetPath(assets[0])));
            }
            else if (assets.Length == 0)
            {
                UnityEngine.Debug.LogError($"Failed to find the source components to copy in your Unity installation");
                return;
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to find source directory to install {azureKinectBodyTrackingSdkComponents[0]}: multiple copies were found in the Unity assets directory");
                return;
            }

            foreach (var component in azureKinectBodyTrackingSdkComponents)
            {
                var sourcePath = Path.Combine(componentSourceDirectory, component);
                if (!File.Exists(sourcePath))
                {
                    UnityEngine.Debug.LogError($"Failed to find component {component}: it is missing from the Unity assets directory {componentSourceDirectory}");
                    return;
                }
            }

            // Run robocopy to perform a file copy operation in an elevated process that can write to Unity's install directory.
            // Use /XC + /XO + /XN to only copy files that don't already exist in the target directory. Unity 2019 began including
            // some of the CUDA dependencies in its installation.
            var arguments = $"\"{componentSourceDirectory}\" \"{AppDomain.CurrentDomain.BaseDirectory}\" {string.Join(" ", azureKinectBodyTrackingSdkComponents)} /XC /XO /XN";
            ProcessStartInfo psi = new ProcessStartInfo("robocopy.exe", arguments);
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            Process.Start(psi).WaitForExit();

            // Setting this value back to null will cause a re-evaluation next update about whether or not these
            // components were actually copied (e.g. if the user accepted the elevation prompt and the copies
            // succeeded).
            isAzureKinectBodyTrackingSdkInstalledInUnity = null;
        }
    }
}