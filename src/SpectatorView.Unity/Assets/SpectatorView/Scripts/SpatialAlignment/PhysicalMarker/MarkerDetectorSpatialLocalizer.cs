// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// SpatialLocalizer that is based on a marker detector.
    /// </summary>
    public abstract class MarkerDetectorSpatialLocalizer : SpatialLocalizer<MarkerDetectorLocalizationSettings>
    {
        protected IMarkerDetector markerDetector = null;

        public override bool TryDeserializeSettings(BinaryReader reader, out MarkerDetectorLocalizationSettings settings)
        {
            return MarkerDetectorLocalizationSettings.TryDeserialize(reader, out settings);
        }

        public override bool TryCreateLocalizationSession(IPeerConnection peerConnection, MarkerDetectorLocalizationSettings settings, out ISpatialLocalizationSession session)
        {
            session = new LocalizationSession(this, settings, peerConnection);
            return true;
        }

        private class LocalizationSession : SpatialLocalizationSession
        {
            /// <inheritdoc />
            public override IPeerConnection Peer => peerConnection;

            private readonly MarkerDetectorSpatialLocalizer localizer;
            private readonly MarkerDetectorLocalizationSettings settings;
            private readonly IPeerConnection peerConnection;
            private readonly MarkerDetectorCoordinateService coordinateService;

            public LocalizationSession(MarkerDetectorSpatialLocalizer localizer, MarkerDetectorLocalizationSettings settings, IPeerConnection peerConnection) : base()
            {
                this.localizer = localizer;
                this.settings = settings;
                this.peerConnection = peerConnection;

                this.localizer.markerDetector.SetMarkerSize(settings.MarkerSize);
                this.coordinateService = new MarkerDetectorCoordinateService(this.localizer.markerDetector, this.localizer.debugLogging);
            }

            /// <inheritdoc />
            public override async Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken)
            {
                localizer.DebugLog("Getting host coordinate");

                ISpatialCoordinate spatialCoordinate = null;
                using (var cancellableCTS = CancellationTokenSource.CreateLinkedTokenSource(defaultCancellationToken, cancellationToken))
                {
                    await coordinateService.TryDiscoverCoordinatesAsync(cancellationToken, new int[] { settings.MarkerID });

                    if (!coordinateService.TryGetKnownCoordinate(settings.MarkerID, out spatialCoordinate))
                    {
                        Debug.LogError("Unexpected failure to discover a marker coordinate");
                    }
                }

                return spatialCoordinate;
            }

            /// <inheritdoc />
            protected override void OnManagedDispose()
            {
                base.OnManagedDispose();
                this.coordinateService.Dispose();
            }
        }
    }
}