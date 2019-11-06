// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
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

        /// <inheritdoc />
        public override async Task<bool> TryRunLocalizationAsync(SpatialCoordinateSystemParticipant participant)
        {
            return await TryRunLocalizationImplAsync(participant);
        }

        /// <inheritdoc />
        public override async Task<bool> TryResetLocalizationAsync(SpatialCoordinateSystemParticipant participant)
        {
            return await TryRunLocalizationImplAsync(participant);
        }

        private async Task<bool> TryRunLocalizationImplAsync(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Marker-based localization started for: {participant?.NetworkConnection?.ToString() ?? "Unknown NetworkConnection"} with marker type {markerType}");

            // Note: We need to send the remote localization message prior to starting marker visual localization. The MarkerVisualSpatialLocalizer won't return until localization has completed.
            Task<bool> remoteTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.NetworkConnection, PeerSpatialLocalizerId, new MarkerVisualDetectorLocalizationSettings());
            Task<bool> localTask = SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.NetworkConnection, LocalSpatialLocalizerId, new MarkerVisualLocalizationSettings());
            await Task.WhenAll(remoteTask, localTask);
            bool localSuccess = await localTask;
            bool remoteSuccess = await remoteTask;
            return localSuccess && remoteSuccess;
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
