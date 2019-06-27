// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MarkerVisualLocalizationInitializer : SpatialLocalizationInitializer
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

        public override Guid PeerSpatialLocalizerId
        {
            get
            {
                switch (markerType)
                {
                    case MarkerType.ArUco:
                        return ArUcoMarkerVisualSpatialLocalizer.DetectorId;
                    case MarkerType.QRCode:
                        return QRCodeMarkerVisualSpatialLocalizer.DetectorId;
                    default:
                        Debug.LogError("Unknown marker type set for localization.");
                        return Guid.Empty;
                }
            }
        }

        private Guid LocalSpatialLocalizerId
        {
            get
            {
                switch (markerType)
                {
                    case MarkerType.ArUco:
                        return ArUcoMarkerVisualSpatialLocalizer.Id;
                    case MarkerType.QRCode:
                        return QRCodeMarkerVisualSpatialLocalizer.Id;
                    default:
                        Debug.LogError("Unknown marker type set for localization.");
                        return Guid.Empty;
                }
            }
        }

        public override void RunLocalization(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Marker-based localization started for: {participant?.SocketEndpoint?.Address ?? "IPAddress unknown"} with marker type {markerType}");

            // Note: We need to send the remote localization message prior to starting marker visual localization. The MarkerVisualSpatialLocalizer won't return until localization has completed.
            SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, PeerSpatialLocalizerId, new MarkerVisualDetectorLocalizationSettings());
            SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.SocketEndpoint, LocalSpatialLocalizerId, new MarkerVisualLocalizationSettings());
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
