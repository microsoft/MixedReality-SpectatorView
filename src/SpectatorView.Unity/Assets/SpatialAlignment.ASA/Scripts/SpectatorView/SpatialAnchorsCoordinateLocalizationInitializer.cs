// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialAnchorsCoordinateLocalizationInitializer : SpatialLocalizationInitializer
    {
        /// <summary>
        /// Configuration for the Azure Spatial Anchors service.
        /// </summary>
        [SerializeField]
        [Tooltip("Configuration for the Azure Spatial Anchors service.")]
        private SpatialAnchorsConfiguration configuration = null;

        public override Guid PeerSpatialLocalizerId => SpatialAnchorsLocalizer.Id;

        public override void RunLocalization(SpatialCoordinateSystemParticipant participant)
        {
            SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.SocketEndpoint, SpatialAnchorsLocalizer.Id, configuration);

            configuration.IsCoordinateCreator = true;
            SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, SpatialAnchorsLocalizer.Id, configuration);
        }
    }
}