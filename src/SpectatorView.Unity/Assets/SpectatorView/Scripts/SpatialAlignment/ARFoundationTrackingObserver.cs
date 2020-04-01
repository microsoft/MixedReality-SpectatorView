// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine.XR.ARFoundation;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for a AR Foundation device.
    /// </summary>
    public class ARFoundationTrackingObserver : TrackingObserver
    {
        /// <inheritdoc/>
        public override TrackingState TrackingState
        {
            get
            {
#if UNITY_EDITOR
                return TrackingState.Tracking;
#else
                if (ARSession.state == ARSessionState.SessionTracking)
                {
                    return TrackingState.Tracking;
                }
                else
                {
                    return TrackingState.LostTracking;
                }
#endif
            }
        }
    }
}
