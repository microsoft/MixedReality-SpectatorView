// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
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
            configuration.IsCoordinateCreator = false;
            Task<bool> localTask = SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.NetworkConnection, SpatialAnchorsLocalizer.Id, configuration);
            configuration.IsCoordinateCreator = true;
            Task<bool> remoteTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.NetworkConnection, SpatialAnchorsLocalizer.Id, configuration);
            await Task.WhenAll(localTask, remoteTask);
            bool localSuccess = await localTask;
            bool remoteSuccess = await remoteTask;
            return localSuccess && remoteSuccess;
        }
    }
}