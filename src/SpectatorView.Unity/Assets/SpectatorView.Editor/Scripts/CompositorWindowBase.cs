// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    internal class CompositorWindowBase<TWindow> : EditorWindowBase<TWindow> where TWindow : EditorWindowBase<TWindow>
    {
        protected const string HolographicCameraDeviceTypeLabel = "Holographic Camera";
        protected const string AppDeviceTypeLabel = "App";
        protected string holographicCameraIPAddress;

        /// <summary>
        /// This list contains localizers that can't be used in the compositor window for video camera filming.
        /// </summary>
        private readonly List<Guid> unsupportedLocalizers = new List<Guid>
        {
            ArUcoMarkerVisualSpatialLocalizer.Id,
            ArUcoMarkerVisualDetectorSpatialLocalizer.Id,
            QRCodeMarkerVisualSpatialLocalizer.Id,
            QRCodeMarkerVisualDetectorSpatialLocalizer.Id,
            new Guid("FB077E49-B855-453F-9FB5-77187B1AF784") // SpatialAnchorsLocalizer (There is no asmdef to reference for ASA)
        };

        private CompositionManager cachedCompositionManager;
        private HolographicCameraObserver cachedHolographicCameraObserver;

        protected int renderFrameWidth;
        protected int renderFrameHeight;
        protected float aspect;
        private float uiFrameWidth = 100;
        private float uiFrameHeight = 100;

        protected const string notConnectedMessage = "Not connected";
        private const string trackingLostStatusMessage = "Tracking lost";
        private const string unknownTrackingStatusMessage = "Unknown tracking state";
        private const string trackingStalledStatusMessage = "No tracking update in over a second";
        private const string locatingSharedSpatialCoordinate = "Locating shared spatial coordinate...";
        private const string notLocatedSharedSpatialCoordinate = "Not located";
        private const string locatedSharedSpatialCoordinateMessage = "Located";
        private const string calibrationLoadedMessage = "Loaded";
        private const string calibrationNotLoadedMessage = "No camera calibration received";
        private const int horizontalFrameRectangleMargin = 50;
        protected const int textureRenderModeComposite = 0;
        protected const int textureRenderModeSplit = 1;
        private const float quadPadding = 4;
        protected const int connectAndDisconnectButtonWidth = 90;
        private const int settingsButtonWidth = 24;
        private Dictionary<string, Rect> buttonRects = new Dictionary<string, Rect>();

        protected Guid selectedLocalizerId = Guid.Empty;

        protected virtual void OnEnable()
        {
            renderFrameWidth = CompositionManager.GetVideoFrameWidth();
            renderFrameHeight = CompositionManager.GetVideoFrameHeight();
            aspect = ((float)renderFrameWidth) / renderFrameHeight;

            Guid.TryParse(PlayerPrefs.GetString(nameof(selectedLocalizerId)), out selectedLocalizerId);
        }

        protected virtual void OnDisable()
        {
            PlayerPrefs.SetString(nameof(selectedLocalizerId), selectedLocalizerId.ToString());
            PlayerPrefs.Save();
        }

        protected void HolographicCameraNetworkConnectionGUI(string deviceTypeLabel, DeviceInfoObserver deviceInfo, SpatialCoordinateSystemParticipant spatialCoordinateSystemParticipant, bool showCalibrationStatus, bool showSpatialLocalization, ref string ipAddressField)
        {
            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;

            CompositionManager compositionManager = GetCompositionManager();

            EditorGUILayout.BeginVertical("Box");
            {
                Color titleColor;
                if (deviceInfo != null &&
                    deviceInfo.NetworkManager != null &&
                    deviceInfo.NetworkManager.IsConnected &&
                    spatialCoordinateSystemParticipant != null &&
                    spatialCoordinateSystemParticipant.PeerDeviceTrackingState == TrackingState.Tracking &&
                    spatialCoordinateSystemParticipant.PeerSpatialCoordinateIsLocated &&
                    !spatialCoordinateSystemParticipant.PeerIsLocatingSpatialCoordinate &&
                    !deviceInfo.IsTrackingStalled &&
                    (!showCalibrationStatus || (compositionManager != null &&
                    compositionManager.IsCalibrationDataLoaded)))
                {
                    titleColor = Color.green;
                }
                else
                {
                    titleColor = Color.yellow;
                }
                RenderTitle(deviceTypeLabel, titleColor);
                ConnectionStatusGUI(deviceInfo, ref ipAddressField);

                GUI.enabled = deviceInfo != null && deviceInfo.NetworkManager != null && deviceInfo.NetworkManager.IsConnected && spatialCoordinateSystemParticipant != null;
                string sharedSpatialCoordinateStatusMessage;
                if (deviceInfo == null || deviceInfo.NetworkManager == null || !deviceInfo.NetworkManager.IsConnected || spatialCoordinateSystemParticipant == null)
                {
                    sharedSpatialCoordinateStatusMessage = notConnectedMessage;
                }
                else if (spatialCoordinateSystemParticipant.PeerDeviceTrackingState == TrackingState.LostTracking)
                {
                    sharedSpatialCoordinateStatusMessage = trackingLostStatusMessage;
                }
                else if (deviceInfo.IsTrackingStalled)
                {
                    sharedSpatialCoordinateStatusMessage = trackingStalledStatusMessage;
                }
                else if (spatialCoordinateSystemParticipant.PeerIsLocatingSpatialCoordinate)
                {
                    sharedSpatialCoordinateStatusMessage = locatingSharedSpatialCoordinate;
                }
                else if (!spatialCoordinateSystemParticipant.PeerSpatialCoordinateIsLocated)
                {
                    sharedSpatialCoordinateStatusMessage = notLocatedSharedSpatialCoordinate;
                }
                else if (spatialCoordinateSystemParticipant.PeerDeviceTrackingState == TrackingState.Unknown)
                {
                    sharedSpatialCoordinateStatusMessage = unknownTrackingStatusMessage;
                }
                else
                {
                    sharedSpatialCoordinateStatusMessage = locatedSharedSpatialCoordinateMessage;
                }

                if (showSpatialLocalization)
                {
                    GUILayout.Label("Shared spatial coordinate status", boldLabelStyle);
                    GUILayout.Label(sharedSpatialCoordinateStatusMessage);
                }

                string calibrationStatusMessage;
                if (compositionManager != null && compositionManager.IsCalibrationDataLoaded)
                {
                    calibrationStatusMessage = calibrationLoadedMessage;
                }
                else
                {
                    calibrationStatusMessage = calibrationNotLoadedMessage;
                }

                EditorGUILayout.Space();
                if (showCalibrationStatus)
                {
                    GUILayout.Label("Calibration status", boldLabelStyle);
                    GUILayout.Label(calibrationStatusMessage);
                }
                else
                {
                    // Empty label to remain as a placeholder for the Calibration status
                    GUILayout.Label(string.Empty, boldLabelStyle);
                    GUILayout.Label(string.Empty);
                }

                if (showSpatialLocalization)
                {
                    EditorGUILayout.Space();

                    GUILayout.Label("Spatial Alignment", boldLabelStyle);

                    GUILayout.Space(4);

                    SpatialLocalizationGUI(deviceTypeLabel, spatialCoordinateSystemParticipant);
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
        }

        private void SpatialLocalizationGUI(string deviceTypeLabel, SpatialCoordinateSystemParticipant spatialCoordinateSystemParticipant)
        {
            string[] localizerNames;
            int selectedLocalizerIndex = 0;
            ISpatialLocalizer[] localizers = null;
            bool wasEnabled = GUI.enabled;

            if (SpatialCoordinateSystemManager.IsInitialized && spatialCoordinateSystemParticipant != null)
            {
                var supportedPeerLocalizers = spatialCoordinateSystemParticipant?.GetPeerSupportedLocalizersAsync();
                if (supportedPeerLocalizers.IsCompleted)
                {
                    // Only use localizers that are supported by both the device and the compositor window
                    localizers = SpatialCoordinateSystemManager.Instance.Localizers.Where(localizer => !unsupportedLocalizers.Contains(localizer.SpatialLocalizerId) && supportedPeerLocalizers.Result.Contains(localizer.SpatialLocalizerId)).ToArray();
                    localizerNames = localizers.Select(localizer => localizer.DisplayName).ToArray();
                    if (localizerNames.Length == 0)
                    {
                        GUI.enabled = false;
                        localizerNames = new string[] { "No supported localization methods found." };
                    }
                    else
                    {
                        for (int i = 0; i < localizers.Length; i++)
                        {
                            if (localizers[i].SpatialLocalizerId == selectedLocalizerId)
                            {
                                selectedLocalizerIndex = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    GUI.enabled = false;
                    localizerNames = new string[] { "Determining supported methods..." };
                }
            }
            else
            {
                GUI.enabled = false;
                localizerNames = new string[] { "Connect to determine supported methods" };
            }

            GUILayout.BeginHorizontal();
            {
                selectedLocalizerIndex = EditorGUILayout.Popup(selectedLocalizerIndex, localizerNames);
                if (localizers != null &&
                    selectedLocalizerIndex < localizers.Length)
                {
                    selectedLocalizerId = localizers[selectedLocalizerIndex].SpatialLocalizerId;
                    SpatialLocalizationSettingsGUI(deviceTypeLabel, selectedLocalizerIndex, localizers);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Locate Shared Spatial Coordinate", "Detects the shared location used to position objects in the same physical location on multiple devices")))
                {
                    CompositorWorldAnchorLocalizationManager.Instance.RunRemoteLocalizationWithWorldAnchorPersistence(spatialCoordinateSystemParticipant, selectedLocalizerId, LoadLocalizerSettings(localizers[selectedLocalizerIndex]));
                }
                GUILayout.Label(string.Empty, GUILayout.Width(settingsButtonWidth));
            }
            GUILayout.EndHorizontal();

            GUI.enabled = wasEnabled;
        }

        private void SpatialLocalizationSettingsGUI(string deviceTypeLabel, int selectedLocalizerIndex, ISpatialLocalizer[] localizers)
        {
            GUIStyle iconButtonStyle = GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
            GUIContent content = new GUIContent(EditorGUIUtility.Load("icons/d__Popup.png") as Texture2D);

            ISpatialLocalizationSettings settings = localizers == null ? null : LoadLocalizerSettings(localizers[selectedLocalizerIndex]);
            IEditableSpatialLocalizationSettings settingsEditor = settings as IEditableSpatialLocalizationSettings;

            bool wasEnabled = GUI.enabled;
            GUI.enabled = settingsEditor != null;

            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, iconButtonStyle, GUILayout.Width(settingsButtonWidth)))
            {
                Action<SpatialLocalizationSettingsEditor> editingCompleted = null;
                Guid localizerId = localizers[selectedLocalizerIndex].SpatialLocalizerId;
                editingCompleted = e =>
                {
                    e.EditingCompleted -= editingCompleted;
                    SaveLocalizerSettings(localizerId, settings);
                };

                var editor = settingsEditor.CreateEditor();
                editor.EditingCompleted += editingCompleted;
                PopupWindow.Show(buttonRects[deviceTypeLabel], editor);
            }

            if (Event.current.type == EventType.Repaint)
            {
                buttonRects[deviceTypeLabel] = GUILayoutUtility.GetLastRect();
            }

            GUI.enabled = wasEnabled;
        }

        protected virtual Rect ComputeCompositeGUIRect(float frameWidth, float frameHeight)
        {
            return GUILayoutUtility.GetRect(frameWidth, frameHeight);
        }

        protected void CompositeTextureGUI(int textureRenderMode)
        {
            UpdateFrameDimensions();

            Rect framesRect = ComputeCompositeGUIRect(uiFrameWidth, uiFrameHeight);

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                CompositionManager compositionManager = GetCompositionManager();
                if (compositionManager != null && compositionManager.TextureManager != null)
                {
                    if (textureRenderMode == textureRenderModeSplit)
                    {
                        Graphics.DrawTexture(framesRect, compositionManager.TextureManager.quadViewOutputTexture, compositionManager.TextureManager.IgnoreAlphaMaterial);
                    }
                    else
                    {
                        Graphics.DrawTexture(framesRect, compositionManager.TextureManager.previewTexture);
                    }
                }
            }
        }

        protected void ConnectionStatusGUI(DeviceInfoObserver deviceInfo, ref string ipAddressField)
        {
            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;

            if (deviceInfo != null && deviceInfo.NetworkManager != null && deviceInfo.NetworkManager.IsConnected)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Connection status", boldLabelStyle);

                    if (GUILayout.Button(new GUIContent("Disconnect", "Disconnects the network connection to the holographic camera."), GUILayout.Width(connectAndDisconnectButtonWidth)))
                    {
                        deviceInfo.NetworkManager.Disconnect();
                    }
                }
                GUILayout.EndHorizontal();

                if (deviceInfo.NetworkManager.ConnectedIPAddress == deviceInfo.DeviceIPAddress)
                {
                    GUILayout.Label($"Connected to {deviceInfo.DeviceName} ({deviceInfo.DeviceIPAddress})");
                }
                else
                {
                    GUILayout.Label($"Connected to {deviceInfo.DeviceName} ({deviceInfo.NetworkManager.ConnectedIPAddress} -> {deviceInfo.DeviceIPAddress})");
                }

                EditorGUILayout.Space();
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    ipAddressField = EditorGUILayout.TextField(ipAddressField);
                    ConnectButtonGUI(ipAddressField, deviceInfo);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(notConnectedMessage);
                EditorGUILayout.Space();
            }
        }

        protected void ConnectButtonGUI(string targetIpString, DeviceInfoObserver remoteDevice)
        {
            string tooltip = string.Empty;
            IPAddress targetIp;
            bool validAddress = ParseAddress(targetIpString, out targetIp);

            if (remoteDevice == null)
            {
                tooltip = $"{nameof(HolographicCameraObserver)} is missing from the scene.";
            }
            else if (!Application.isPlaying)
            {
                tooltip = "The scene must be in play mode to connect.";
            }
            else if (!validAddress)
            {
                tooltip = "The IP address for the remote device is not valid.";
            }

            GUI.enabled = validAddress && Application.isPlaying && remoteDevice != null;
            string label = remoteDevice != null && remoteDevice.NetworkManager != null && remoteDevice.NetworkManager.IsConnecting ? "Disconnect" : "Connect";
            if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Width(connectAndDisconnectButtonWidth)) && remoteDevice != null)
            {
                if (remoteDevice.NetworkManager.IsConnecting)
                {
                    remoteDevice.NetworkManager.Disconnect();
                }
                else
                {
                    remoteDevice.NetworkManager.ConnectTo(targetIpString);
                }
            }
            GUI.enabled = true;
        }

        private bool ParseAddress(string targetIpString, out IPAddress targetIp)
        {
            if (targetIpString == "localhost")
            {
                targetIp = IPAddress.Loopback;
                return true;
            }
            else
            {
                return IPAddress.TryParse(targetIpString, out targetIp);
            }
        }

        private void UpdateFrameDimensions()
        {
            uiFrameWidth = position.width - horizontalFrameRectangleMargin;
            uiFrameHeight = position.height;

            if (uiFrameWidth <= uiFrameHeight * aspect)
            {
                uiFrameHeight = uiFrameWidth / aspect;
            }
            else
            {
                uiFrameWidth = uiFrameHeight * aspect;
            }
        }

        protected CompositionManager GetCompositionManager()
        {
            if (cachedCompositionManager == null)
            {
                cachedCompositionManager = FindObjectOfType<CompositionManager>();
            }

            return cachedCompositionManager;
        }

        protected HolographicCameraObserver GetHolographicCameraObserver()
        {
            if (cachedHolographicCameraObserver == null)
            {
                cachedHolographicCameraObserver = FindObjectOfType<HolographicCameraObserver>();
            }

            return cachedHolographicCameraObserver;
        }

        protected DeviceInfoObserver GetHolographicCameraDevice()
        {
            HolographicCameraObserver observer = GetHolographicCameraObserver();
            if (observer != null)
            {
                return observer.GetComponent<DeviceInfoObserver>();
            }
            else
            {
                return null;
            }
        }

        protected SpatialCoordinateSystemParticipant GetSpatialCoordinateSystemParticipant(DeviceInfoObserver device)
        {
            if (device != null && device.NetworkConnection != null && SpatialCoordinateSystemManager.IsInitialized)
            {
                if (SpatialCoordinateSystemManager.Instance.TryGetSpatialCoordinateSystemParticipant(device.NetworkConnection, out SpatialCoordinateSystemParticipant participant))
                {
                    return participant;
                }
                else
                {
                    Debug.LogError("Expected to be able to find participant for an endpoint");
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private void SaveLocalizerSettings(Guid spatialLocalizerId, ISpatialLocalizationSettings settings)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                settings.Serialize(writer);
                PlayerPrefs.SetString(spatialLocalizerId.ToString(), Convert.ToBase64String(stream.ToArray()));
                PlayerPrefs.Save();
            }
        }

        private ISpatialLocalizationSettings LoadLocalizerSettings(ISpatialLocalizer spatialLocalizer)
        {
            ISpatialLocalizationSettings settings;
            string settingsString = PlayerPrefs.GetString(spatialLocalizer.SpatialLocalizerId.ToString(), null);
            if (string.IsNullOrEmpty(settingsString))
            {
                settings = spatialLocalizer.CreateDefaultSettings();
            }
            else
            {
                byte[] settingsBytes = Convert.FromBase64String(settingsString);
                using (MemoryStream stream = new MemoryStream(settingsBytes))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (!spatialLocalizer.TryDeserializeSettings(reader, out settings))
                    {
                        Debug.LogError($"Failed to deserialize settings for spatial localizer {spatialLocalizer.DisplayName} ({spatialLocalizer.SpatialLocalizerId}), using default settings");
                        settings = spatialLocalizer.CreateDefaultSettings();
                    }
                }
            }

            return settings;
        }
    }
}