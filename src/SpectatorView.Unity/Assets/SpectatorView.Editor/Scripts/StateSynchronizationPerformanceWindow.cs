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
                        RenderTitle("StateSynchronizationObserver was not detected in the current scene. Open the SpectatorViewCompositor scene.", Color.red);
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

            if (StateSynchronizationObserver.Instance.PerformanceFeatureCount == 0)
            {
                RenderTitle("Waiting for performance information...", Color.yellow);
            }
            else
            {
                RenderTitle("HoloLens application performance information", Color.green);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Enable Diagnostic Mode", "Turns on performance diagnostic mode for the attached HoloLens.")))
                {
                    StateSynchronizationObserver.Instance.SetPerformanceDiagnosticMode(true);
                }
                if (GUILayout.Button(new GUIContent("Disable Diagnostic Mode", "Turns off performance diagnostic mode for the attached HoloLens.")))
                {
                    StateSynchronizationObserver.Instance.SetPerformanceDiagnosticMode(false);
                }
                GUILayout.EndHorizontal();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                GUILayout.Label($"Performance Diagnostic Mode Enabled:{StateSynchronizationObserver.Instance.PerformanceDiagnosticModeEnabled}");
                IReadOnlyList<double> times = StateSynchronizationObserver.Instance.AverageTimePerFeature;
                for (int i = 0; i < times.Count; i++)
                {
                    double time = times[i];
                    GUILayout.Label($"Feature {(StateSynchronizationPerformanceFeature)i}:{time.ToString("G4")}");
                }

                IReadOnlyList<string> updatedProperties = StateSynchronizationObserver.Instance.UpdatedPropertyDetails;
                if (updatedProperties != null &&
                    updatedProperties.Count > 0)
                {
                    RenderTitle("Diagnostic Details", Color.green);
                    GUILayout.Label("Updated Material Properties:");
                    foreach (var updatedProperty in updatedProperties)
                    {
                        GUILayout.Label(updatedProperty);
                    }
                }

                GUILayout.Space(defaultSpacing);
                GUILayout.Label($"Material Update Count:{StateSynchronizationObserver.Instance.MaterialUpdateCount}");

                EditorGUILayout.EndScrollView();
            }
        }
    }
}
