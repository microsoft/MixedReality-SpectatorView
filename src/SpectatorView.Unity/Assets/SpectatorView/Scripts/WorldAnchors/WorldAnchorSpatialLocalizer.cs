using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MixedReality.Experimental.SpatialAlignment.Common;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.WorldAnchors
{
    public class WorldAnchorSpatialLocalizer : SpatialLocalizer<WorldAnchorSpatialLocalizationSettings>
    {
        public static readonly Guid Id = new Guid("0858173D-B0F4-4D19-9B33-CADC1EFC96FE");

        public override Guid SpatialLocalizerId => Id;

        private Task<WorldAnchorCoordinateService> coordinateServiceTask;

        protected override void Start()
        {
            base.Start();

            coordinateServiceTask = WorldAnchorCoordinateService.GetSharedCoordinateServiceAsync();
        }

        public override bool TryDeserializeSettings(BinaryReader reader, out WorldAnchorSpatialLocalizationSettings settings)
        {
            return WorldAnchorSpatialLocalizationSettings.TryDeserialize(reader, out settings);
        }

        public override bool TryCreateLocalizationSession(IPeerConnection peerConnection, WorldAnchorSpatialLocalizationSettings settings, out ISpatialLocalizationSession session)
        {
            session = new LocalizationSession(this, settings);
            return true;
        }

        private class LocalizationSession : DisposableBase, ISpatialLocalizationSession
        {
            private readonly WorldAnchorSpatialLocalizer localizer;
            private readonly WorldAnchorSpatialLocalizationSettings settings;

            public LocalizationSession(WorldAnchorSpatialLocalizer localizer, WorldAnchorSpatialLocalizationSettings settings)
            {
                this.localizer = localizer;
                this.settings = settings;
            }

            public async Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken)
            {
                WorldAnchorCoordinateService coordinateService = await localizer.coordinateServiceTask.Unless(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (settings.Mode == WorldAnchorLocalizationMode.LocateExistingAnchor)
                {
                    if (coordinateService.TryGetKnownCoordinate(settings.AnchorId, out ISpatialCoordinate coordinate))
                    {
                        return coordinate;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return await coordinateService.CreateCoordinateAsync(settings.AnchorId, settings.AnchorPosition, settings.AnchorRotation, cancellationToken);
                }
            }

            public void OnDataReceived(BinaryReader reader)
            {
            }
        }
    }
}