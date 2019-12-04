// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for a HoloLens device.
    /// </summary>
    public class HoloLensTrackingObserver : TrackingObserver
    {
        /// <inheritdoc/>
        public override TrackingState TrackingState
        {
            get
            {
#if UNITY_EDITOR
                return TrackingState.Tracking;
#elif UNITY_WSA
                if (UnityEngine.XR.WSA.WorldManager.state == UnityEngine.XR.WSA.PositionalLocatorState.Active)
                {
                    return TrackingState.Tracking;
                }

                return TrackingState.LostTracking;
#else
                return TrackingState.Unknown;
#endif
            }
        }
    }
}