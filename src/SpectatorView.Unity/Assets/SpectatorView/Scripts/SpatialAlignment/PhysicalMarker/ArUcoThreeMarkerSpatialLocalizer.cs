// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ArUcoThreeMarkerSpatialLocalizer : SpatialLocalizer<ArUcoThreeMarkerLocalizationSettings>
    {
        public static readonly Guid Id = new Guid("79FD4A8C-2BB3-495E-83BD-0681E987739E");

        public override Guid SpatialLocalizerId => Id;

        [Tooltip("ArUco marker detector used by the spatial localizer.")]
        [SerializeField]
        private ArUcoMarkerDetector markerDetector = null;

        public override string DisplayName => "ArUco Three Marker";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_EDITOR
                return true;
#elif UNITY_WSA
                return Windows.ApplicationModel.Package.Current.Id.Architecture == Windows.System.ProcessorArchitecture.X86;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            DebugLog("Awake");

            if (markerDetector == null)
            {
                Debug.LogWarning("Marker detector not appropriately set for ArUcoThreeMarkerSpatialLocalizer");
            }
        }

        public override bool TryDeserializeSettings(BinaryReader reader, out ArUcoThreeMarkerLocalizationSettings settings)
        {
            return ArUcoThreeMarkerLocalizationSettings.TryDeserialize(reader, out settings);
        }

        public override bool TryCreateLocalizationSession(IPeerConnection peerConnection, ArUcoThreeMarkerLocalizationSettings settings, out ISpatialLocalizationSession session)
        {
            session = new LocalizationSession(this, settings, peerConnection);
            return true;
        }

        private class LocalizationSession : SpatialLocalizationSession
        {
            /// <inheritdoc />
            public override IPeerConnection Peer => peerConnection;

            private readonly ArUcoThreeMarkerSpatialLocalizer localizer;
            private readonly ArUcoThreeMarkerLocalizationSettings settings;
            private readonly IPeerConnection peerConnection;
            private readonly MarkerDetectorCoordinateService coordinateService;

            public LocalizationSession(ArUcoThreeMarkerSpatialLocalizer localizer, ArUcoThreeMarkerLocalizationSettings settings, IPeerConnection peerConnection) : base()
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
                    await coordinateService.TryDiscoverCoordinatesAsync(cancellationToken, new int[] { settings.TopMarkerID, settings.MiddleMarkerID, settings.BottomMarkerID });

                    if (!coordinateService.TryGetKnownCoordinate(settings.TopMarkerID, out ISpatialCoordinate topSpatialCoordinate) ||
                        !coordinateService.TryGetKnownCoordinate(settings.MiddleMarkerID, out ISpatialCoordinate middleSpatialCoordinate) ||
                        !coordinateService.TryGetKnownCoordinate(settings.BottomMarkerID, out ISpatialCoordinate bottomSpatialCoordinate))
                    {
                        Debug.LogError("Unexpected failure to discover a marker coordinate");
                    }
                    else
                    {
                        var topPosition = topSpatialCoordinate.CoordinateToWorldSpace(Vector3.zero);
                        var middlePosition = middleSpatialCoordinate.CoordinateToWorldSpace(Vector3.zero);
                        var bottomPosition = bottomSpatialCoordinate.CoordinateToWorldSpace(Vector3.zero);

                        // Find the point that is at the T intersection of the three markers by projecting the middle marker
                        // onto the top-bottom line segment
                        var markerIntersection = bottomPosition + Vector3.Project(middlePosition - bottomPosition, topPosition - bottomPosition);

                        // Create a stable rotation for the overall marker using the plane formed by the three
                        // individual markers. The order of points passed to the Plane constructor is important to 
                        // ensure the normal direction is the correct direction to create the quaternion.
                        var markerPlane = new Plane(topPosition, middlePosition, bottomPosition);
                        var markerRotation = Quaternion.LookRotation(markerPlane.normal, bottomPosition - topPosition);

                        spatialCoordinate = new SpatialCoordinate(markerIntersection, markerRotation);
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

            private class SpatialCoordinate : SpatialCoordinateUnityBase<int>
            {
                public SpatialCoordinate(Vector3 worldPosition, Quaternion worldRotation) : base(0)
                {
                    SetCoordinateWorldTransform(worldPosition, worldRotation);
                }

                public override LocatedState State => LocatedState.Tracking;
            }
        }
    }
}