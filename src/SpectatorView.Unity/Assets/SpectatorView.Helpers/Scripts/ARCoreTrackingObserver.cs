// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for an ARCore device.
    /// </summary>
    public class ARCoreTrackingObserver : TrackingObserver
    {
        /// <inheritdoc/>
        public override TrackingState TrackingState
        {
            get
            {
#if UNITY_ANDROID
                if (GoogleARCore.Session.Status == GoogleARCore.SessionStatus.Tracking)
                {
                    return TrackingState.Tracking;
                }
                
                return TrackingState.LostTracking;
#elif UNITY_EDITOR
                return TrackingState.Tracking;
#else
                return TrackingState.Unknown;
#endif
            }
        }
    }
}