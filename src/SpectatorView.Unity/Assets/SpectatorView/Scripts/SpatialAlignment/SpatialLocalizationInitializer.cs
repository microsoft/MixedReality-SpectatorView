using System;
using System.Collections;
using System.Collections.Generic;
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

        public abstract void RunLocalization(SpatialCoordinateSystemParticipant participant);
    }
}