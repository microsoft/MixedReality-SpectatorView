// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class MarkerVisualLocalizationInitializer : MonoBehaviour
    {
        private enum MarkerType
        {
            ArUco,
            QRCode
        }

        [SerializeField]
        private bool debugLogging = false;

        [SerializeField]
        private MarkerType markerType = MarkerType.ArUco;

        private void Awake()
        {
            DebugLog("Registering ParticipantConnected event.");
            SpatialCoordinateSystemManager.Instance.ParticipantConnected += ParticipantConnected;
        }

        private void OnDestroy()
        {
            DebugLog("Registering ParticipantConnected event.");
            SpatialCoordinateSystemManager.Instance.ParticipantConnected -= ParticipantConnected;
        }

        private void ParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Participant connected: {participant?.SocketEndpoint?.Address ?? "IPAddress unknown"}");

            // Note: We need to send the remote localization message prior to starting marker visual localization. The MarkerVisualSpatialLocalizer won't return until localization has completed.
            switch (markerType)
            {
                case MarkerType.ArUco:
                    DebugLog("Starting ArUco marker based localization.");
                    SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, ArUcoMarkerVisualSpatialLocalizer.DetectorId, new MarkerVisualDetectorLocalizationSettings());
                    SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.SocketEndpoint, ArUcoMarkerVisualSpatialLocalizer.Id, new MarkerVisualLocalizationSettings());
                    break;
                case MarkerType.QRCode:
                    DebugLog("Starting QR Code based localization.");
                    SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, QRCodeMarkerVisualSpatialLocalizer.DetectorId, new MarkerVisualDetectorLocalizationSettings());
                    SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.SocketEndpoint, QRCodeMarkerVisualSpatialLocalizer.Id, new MarkerVisualLocalizationSettings());
                    break;
                default:
                    Debug.LogError("Uknown marker type set for localization.");
                    break;
            }
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"MarkerVisualLocalizationInitializer: {message}");
            }
        }
    }
}
