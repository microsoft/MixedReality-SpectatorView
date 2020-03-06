// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public abstract class SpatialLocalizationInitializer : MonoBehaviour
    {
        /// <summary>
        /// Gets the ID of the ISpatialLocalizer that should be run on the connected
        /// peer device to initiate localization.
        /// </summary>
        public abstract Guid PeerSpatialLocalizerId { get; }

        /// <summary>
        /// Call to attempt localization with for provided participant.
        /// </summary>
        /// <param name="participant">participant to localize with</param>
        /// <returns>True if localization succeeded, otherwise false</returns>
        public abstract Task<bool> TryRunLocalizationAsync(SpatialCoordinateSystemParticipant participant);

        /// <summary>
        /// Call to attempt relocalization with for provided participant.
        /// </summary>
        /// <param name="participant">participant to localize with</param>
        /// <returns>True if relocalization succeeded, otherwise false</returns>
        public abstract Task<bool> TryResetLocalizationAsync(SpatialCoordinateSystemParticipant participant);
    }
}