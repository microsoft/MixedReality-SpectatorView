// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MarkerVisualDetectorLocalizationSettings : ISpatialLocalizationSettings
    {
        public MarkerVisualDetectorLocalizationSettings() { }

        public void Serialize(BinaryWriter writer) { }
    }

    public abstract class MarkerVisualDetectorSpatialLocalizer : SpatialLocalizer<MarkerVisualDetectorLocalizationSettings>
    {
        [Tooltip("The reference to an IMarkerDetector GameObject")]
        [SerializeField]
        protected MonoBehaviour MarkerDetector = null;
        private IMarkerDetector markerDetector = null;

        /// <inheritdoc />
        public override bool TryCreateLocalizationSession(IPeerConnection peerConnection, MarkerVisualDetectorLocalizationSettings settings, out ISpatialLocalizationSession session)
        {
            markerDetector = (markerDetector == null) ? MarkerDetector as IMarkerDetector : markerDetector;
            session = new LocalizationSession(this, settings, peerConnection, debugLogging);
            return true;
        }

        /// <inheritdoc />
        public override bool TryDeserializeSettings(BinaryReader reader, out MarkerVisualDetectorLocalizationSettings settings)
        {
            settings = new MarkerVisualDetectorLocalizationSettings();
            return true;
        }

        private class LocalizationSession : SpatialLocalizationSession
        {
            /// <inheritdoc />
            public override IPeerConnection Peer => peerConnection;

            private readonly MarkerVisualDetectorSpatialLocalizer localizer;
            private readonly MarkerVisualDetectorLocalizationSettings settings;
            private readonly IPeerConnection peerConnection;
            private readonly ISpatialCoordinateService coordinateService;
            private readonly bool debugLogging = false;
            private readonly TaskCompletionSource<string> coordinateAssigned = null;
            private readonly CancellationTokenSource discoveryCTS = null;

            private string coordinateId = string.Empty;

            public LocalizationSession(MarkerVisualDetectorSpatialLocalizer localizer, MarkerVisualDetectorLocalizationSettings settings, IPeerConnection peerConnection, bool debugLogging = false) : base()
            {
                DebugLog("Session created");
                this.localizer = localizer;
                this.settings = settings;
                this.peerConnection = peerConnection;
                this.debugLogging = debugLogging;

                this.coordinateAssigned = new TaskCompletionSource<string>();
                this.coordinateService = new MarkerDetectorCoordinateService(this.localizer.markerDetector, debugLogging);
                this.discoveryCTS = new CancellationTokenSource();
            }

            /// <inheritdoc />
            protected override void OnManagedDispose()
            {
                base.OnManagedDispose();
                discoveryCTS.Dispose();
                coordinateService.Dispose();
            }

            /// <inheritdoc />
            public override async Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken)
            {
                if (!defaultCTS.Token.CanBeCanceled)
                {
                    Debug.LogError("Session is invalid. No localiation performed.");
                    return null;
                }

                DebugLog($"Waiting for marker visual, CanBeCanceled:{cancellationToken.CanBeCanceled}, IsCancellationRequested:{cancellationToken.IsCancellationRequested}");
                using (var cancellableCTS = CancellationTokenSource.CreateLinkedTokenSource(defaultCTS.Token, cancellationToken))
                {
                    await Task.WhenAny(coordinateAssigned.Task, Task.Delay(-1, cancellableCTS.Token));
                    if (string.IsNullOrEmpty(coordinateId))
                    {
                        DebugLog("Failed to assign coordinate id");
                        return null;
                    }

                    ISpatialCoordinate coordinate = null;
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(discoveryCTS.Token, cancellableCTS.Token))
                    {
                        DebugLog($"Attempting to discover coordinate: {coordinateId}, CanBeCanceled:{cts.Token.CanBeCanceled}, IsCancellationRequested:{cts.Token.IsCancellationRequested}");
                        if (await coordinateService.TryDiscoverCoordinatesAsync(cts.Token, new string[] { coordinateId.ToString() }))
                        {
                            DebugLog($"Coordinate discovery completed: {coordinateId}");
                            if (!coordinateService.TryGetKnownCoordinate(coordinateId, out coordinate))
                            {
                                DebugLog("Failed to find spatial coordinate although discovery completed.");
                            }
                            else
                            {
                                SendCoordinateFound(coordinate.Id);
                                return coordinate;
                            }
                        }
                        else
                        {
                            DebugLog("TryDiscoverCoordinatesAsync failed.");
                        }
                    }
                }

                return null;
            }

            /// <inheritdoc />
            public override void OnDataReceived(BinaryReader reader)
            {
                string command = reader.ReadString();
                DebugLog($"Received command: {command}");
                switch (command)
                {
                    case MarkerVisualLocalizationSettings.DiscoveryHeader:
                        int maxSupportedMarkerId = reader.Read();
                        coordinateId = DetermineCoordinateId(maxSupportedMarkerId);
                        SendCoordinateAssigned(coordinateId);
                        coordinateAssigned.TrySetResult(coordinateId);
                        break;
                    default:
                        DebugLog($"Sent unknown command: {command}");
                        break;
                }
            }

            private void DebugLog(string message)
            {
                if (debugLogging)
                {
                    Debug.Log($"MarkerVisualDetectorSpatialLocalizer.LocalizationSession: {message}");
                }
            }

            private void SendCoordinateAssigned(string coordinateId)
            {
                DebugLog($"Sending coordinate assignment: {coordinateId}");
                peerConnection.SendData(writer =>
                {
                    writer.Write(MarkerVisualLocalizationSettings.CoordinateAssignedHeader);
                    writer.Write(coordinateId);
                });
            }

            private void SendCoordinateFound(string coordinateId)
            {
                DebugLog($"Sending coordinate found: {coordinateId}");
                peerConnection.SendData(writer =>
                {
                    writer.Write(MarkerVisualLocalizationSettings.CoordinateFoundHeader);
                    writer.Write(coordinateId);
                });
            }

            private string DetermineCoordinateId(int maxSupportedMarkerId)
            {
                DebugLog("GetMarkerId currently returns 1 when possible to avoid conficts with the MarkerDetectorSpatialLocalizer that uses 0. Additional work is still required to enable assigning unique marker ids to different application participants.");
                if (maxSupportedMarkerId > 0)
                {
                    return 1.ToString();
                }

                return 0.ToString();
            }
        }
    }
}
