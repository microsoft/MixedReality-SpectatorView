// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// An <see cref="ISpatialLocalizer"/> that can locate spatial coordinates based on
    /// WorldAnchors stored in the device's WorldAnchorStore.
    /// </summary>
    public class WorldAnchorSpatialLocalizer : SpatialLocalizer<WorldAnchorSpatialLocalizationSettings>
    {
        public static readonly Guid Id = new Guid("0858173D-B0F4-4D19-9B33-CADC1EFC96FE");
        private Task<WorldAnchorCoordinateService> coordinateServiceTask;

        /// <inheritdoc />
        public override Guid SpatialLocalizerId => Id;

        /// <inheritdoc />
        public override string DisplayName => "World Anchor";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_WSA && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        protected override void Start()
        {
            base.Start();

            // Initialize the shared coordinate service during Start. This way, WorldAnchors
            // can be located and tracking even before a connection is formed to the device.
            // This will reduce the amount of time needed to located anchors once a client
            // does connect to the device.
            coordinateServiceTask = WorldAnchorCoordinateService.GetSharedCoordinateServiceAsync();
        }

        /// <inheritdoc />
        public override bool TryDeserializeSettings(BinaryReader reader, out WorldAnchorSpatialLocalizationSettings settings)
        {
            return WorldAnchorSpatialLocalizationSettings.TryDeserialize(reader, out settings);
        }

        /// <inheritdoc />
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