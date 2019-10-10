// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using UnityEditor;
using System.ComponentModel;
using System.Collections.Generic;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    [Description("Performance")]
    internal class StateSynchronizationPerformanceWindow : CompositorWindowBase<StateSynchronizationPerformanceWindow>
    {
        private static readonly string appIPAddressKey = $"{nameof(StateSynchronizationPerformanceWindow)}.{nameof(appIPAddress)}";
        private string appIPAddress;
        private const int globalSettingsButtonWidth = 220;
        private Vector2 scrollPosition;
        private const int defaultSpacing = 10;

        [MenuItem("Spectator View/Performance", false, 3)]
        public static void ShowCalibrationRecordingWindow()
        {
            ShowWindow();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            appIPAddress = PlayerPrefs.GetString(appIPAddressKey, "localhost");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PlayerPrefs.SetString(appIPAddressKey, appIPAddress);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button(new GUIContent("Open performance settings prefab", "Opens the prefab that defines global performance settings."), GUILayout.Width(globalSettingsButtonWidth)))
                {
                    StateSynchronizationMenuItems.EditGlobalPerformanceParameters();
                }

                if (!EditorApplication.isPlaying)
                {
                    if (StateSynchronizationObserver.Instance == null)
                    {
                        RenderTitle("StateSynchronizationObserver was not detected in the current scene. Open the SpectatorViewPerformance scene.", Color.red);
                    }
                    else
                    {
                        RenderTitle("Enter playmode to view performance information.", Color.gray);
                    }
                }
                else if (EditorApplication.isPlaying &&
                    !StateSynchronizationObserver.IsInitialized)
                {
                    RenderTitle("StateSynchronizationObserver was not detected in the current scene. Open SpectatorViewCompositor", Color.red);
                }
                else
                {
                    UpdatePerformanceInformation();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void UpdatePerformanceInformation()
        {
            DeviceInfoObserver stateSynchronizationDevice = null;
            if (StateSynchronizationObserver.IsInitialized)
            {
                stateSynchronizationDevice = StateSynchronizationObserver.Instance.GetComponent<DeviceInfoObserver>();
            }

            HolographicCameraNetworkConnectionGUI(
                AppDeviceTypeLabel,
                stateSynchronizationDevice,
                GetSpatialCoordinateSystemParticipant(stateSynchronizationDevice),
                showCalibrationStatus: false,
                showSpatialLocalization: false,
                ref appIPAddress);

            RenderTitle("HoloLens application performance information", Color.green);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Enable Performance Monitoring", "Turns on performance monitoring mode for the attached HoloLens.")))
            {
                StateSynchronizationObserver.Instance.SetPerformanceMonitoringMode(true);
            }
            if (GUILayout.Button(new GUIContent("Disable Performance Monitoring", "Turns off performance diagnostic mode for the attached HoloLens.")))
            {
                StateSynchronizationObserver.Instance.SetPerformanceMonitoringMode(false);
            }
            GUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label($"Performance Diagnostic Mode Enabled:{StateSynchronizationObserver.Instance.PerformanceMonitoringModeEnabled}");
            if (StateSynchronizationObserver.Instance.PerformanceMonitoringModeEnabled)
            {
                if (StateSynchronizationObserver.Instance.PerformanceEventDurations != null)
                {
                    RenderTitle("Event Durations (ms)", Color.green);
                    foreach (var duration in StateSynchronizationObserver.Instance.PerformanceEventDurations)
                    {
                        GUILayout.Label($"{duration.Item1}:{duration.Item2.ToString("G4")}");
                    }
                }

                if (StateSynchronizationObserver.Instance.PerformanceEventCounts != null)
                {
                    GUILayout.Space(defaultSpacing);
                    RenderTitle("Event Counts", Color.green);
                    foreach (var count in StateSynchronizationObserver.Instance.PerformanceEventCounts)
                    {
                        GUILayout.Label($"{count.Item1}:{count.Item2}");
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
