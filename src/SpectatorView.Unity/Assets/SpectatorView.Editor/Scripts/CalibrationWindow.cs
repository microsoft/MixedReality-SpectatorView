using System.Collections;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    [Description("Calibration")]
    internal class CalibrationWindow : CompositorWindowBase<CalibrationWindow>
    {
        private Vector2 scrollPosition;
        private static readonly string holographicCameraIPAddressKey = $"{nameof(CalibrationWindow)}.{nameof(holographicCameraIPAddress)}";
        private const float scrollBarWidth = 30.0f;
        private const float buttonWidth = 200.0f;

        [MenuItem("Spectator View/Calibration", false, 1)]
        public static void ShowCalibrationRecordingWindow()
        {
            ShowWindow();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            holographicCameraIPAddress = PlayerPrefs.GetString(holographicCameraIPAddressKey, "localhost");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PlayerPrefs.SetString(holographicCameraIPAddressKey, holographicCameraIPAddress);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                var intrinsics = FindObjectOfType<EditorIntrinsicsCalibration>();
                if (intrinsics != null)
                {
                    UpdateIntrinsicsUI(intrinsics);
                }

                var extrinsics = FindObjectOfType<EditorExtrinsicsCalibration>();
                if (extrinsics != null)
                {
                    UpdateExtrinsicsUI(extrinsics);
                }

                if (intrinsics == null &&
                    extrinsics == null)
                {
                    RenderTitle("No calibration components were detected in the current scene.", Color.red);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void UpdateIntrinsicsUI(EditorIntrinsicsCalibration intrinsics)
        {
            RenderTitle("Camera Intrinsics", Color.green);
            GUILayout.Label($"Usable Chessboard Images: {intrinsics.ProcessedImages}");
            EditorGUILayout.Space();

            GUI.enabled = CompositorWrapper.IsInitialized;
            if (GUILayout.Button(new GUIContent("Take Photo", "Takes a photo with the DSLR camera and searches for a chessboard."), GUILayout.Width(buttonWidth)))
            {
                intrinsics.TakePhoto();
            }

            GUI.enabled = intrinsics.ProcessedImages > 0;
            if (GUILayout.Button(new GUIContent("Calculate Camera Intrinsics", "Calculates the camera intrinsics using all images that contained chessboards."), GUILayout.Width(buttonWidth)))
            {
                intrinsics.CalculateCameraIntrinsics();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            if (intrinsics.IntrinsicsFileName != string.Empty &&
                intrinsics.Intrinsics != null)
            {
                RenderTitle(intrinsics.IntrinsicsFileName, Color.green);
                GUILayout.Label($"Reprojection Error: {intrinsics.Intrinsics.ReprojectionError.ToString("G4")}");
                GUILayout.Label($"Focal Length: {intrinsics.Intrinsics.FocalLength.ToString("G4")}, Principal Point: {intrinsics.Intrinsics.PrincipalPoint.ToString("G4")}");
                GUILayout.Label($"Radial Distortion: {intrinsics.Intrinsics.RadialDistortion.ToString("G4")}, Tangential Distortion: {intrinsics.Intrinsics.TangentialDistortion.ToString("G4")}");
            }
        }

        private void UpdateExtrinsicsUI(EditorExtrinsicsCalibration extrinsics)
        {
            RenderTitle("Camera Extrinsics", Color.green);
            GUILayout.Label($"Usable marker datasets: {extrinsics.ProcessedDatasets}");
            GUILayout.Label($"Number of detected markers in last dataset: {extrinsics.LastCountDetectedMarkers}, minimum required: {extrinsics.MinimumNumberOfDetectedMarkers}");
            EditorGUILayout.Space();

            GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;

            RenderTitle(HolographicCameraDeviceTypeLabel, Color.green);
            var cameraDevice = GetHolographicCameraDevice();
            if (cameraDevice != null && cameraDevice.NetworkManager != null && cameraDevice.NetworkManager.IsConnected)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Connection status", boldLabelStyle);

                    if (GUILayout.Button(new GUIContent("Disconnect", "Disconnects the network connection to the holographic camera."), GUILayout.Width(connectAndDisconnectButtonWidth)))
                    {
                        cameraDevice.NetworkManager.Disconnect();
                    }
                }
                GUILayout.EndHorizontal();

                if (cameraDevice.NetworkManager.ConnectedIPAddress == cameraDevice.DeviceIPAddress)
                {
                    GUILayout.Label($"Connected to {cameraDevice.DeviceName} ({cameraDevice.DeviceIPAddress})");
                }
                else
                {
                    GUILayout.Label($"Connected to {cameraDevice.DeviceName} ({cameraDevice.NetworkManager.ConnectedIPAddress} -> {cameraDevice.DeviceIPAddress})");
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    holographicCameraIPAddress = EditorGUILayout.TextField(holographicCameraIPAddress);
                    ConnectButtonGUI(holographicCameraIPAddress, cameraDevice);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(notConnectedMessage);
            }

            EditorGUILayout.Space();

            GUI.enabled = cameraDevice != null && cameraDevice.NetworkManager != null && cameraDevice.NetworkManager.IsConnected;
            if (GUILayout.Button(new GUIContent("Request Marker Data", "Sends a message to the Holographic Camera hololens requesting marker dataset for calibration."), GUILayout.Width(buttonWidth)))
            {
                extrinsics.RequestHeadsetData();
            }

            GUI.enabled = extrinsics.ProcessedDatasets > 0;
            if (GUILayout.Button(new GUIContent("Calculate Camera Extrinsics", "Calculates the camera extrinsics using all valid marker datasets."), GUILayout.Width(buttonWidth)))
            {
                extrinsics.CalculateExtrinsics();
            }
            GUI.enabled = true;

            if (extrinsics.GlobalExtrinsicsFileName != string.Empty &&
                extrinsics.GlobalExtrinsics != null)
            {
                EditorGUILayout.Space();
                RenderTitle(extrinsics.GlobalExtrinsicsFileName, Color.green);
                GUILayout.Label($"Calculation succeeded: {extrinsics.GlobalExtrinsics.Succeeded}");
                GUILayout.Label("View From World:");
                GUILayout.Label($"{extrinsics.GlobalExtrinsics.ViewFromWorld.ToString("G4")}");
                GUILayout.Label($"Note: 'Global Extrinsics' and 'Global HoloLens' GameObjects have been added to the Unity scene to demonstrate the calculated offset between the DSLR Camera and HoloLens");
                EditorGUILayout.Space();
            }

            if (extrinsics.CalibrationFileName != string.Empty)
            {
                RenderTitle(extrinsics.CalibrationFileName, Color.green);
            }

            GUI.enabled = extrinsics.CalibrationFileName != string.Empty &&
                cameraDevice != null &&
                cameraDevice.NetworkManager != null &&
                cameraDevice.NetworkManager.IsConnected;
            if (GUILayout.Button(new GUIContent("Upload Calibration Data", "Sends calibration data to the connected HoloLens device."), GUILayout.Width(buttonWidth)))
            {
                extrinsics.UploadCalibrationData();
            }
            GUI.enabled = true;

            if (extrinsics.UploadResultMessage != string.Empty)
            {
                Color color = extrinsics.UploadSucceeded ? Color.green : Color.red;
                RenderTitle(extrinsics.UploadResultMessage, color);
            }
        }
    }
}
