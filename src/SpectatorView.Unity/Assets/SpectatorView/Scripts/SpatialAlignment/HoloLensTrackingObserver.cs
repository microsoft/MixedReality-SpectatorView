using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for a HoloLens device.
    /// </summary>
    public class HoloLensTrackingObserver : MonoBehaviour, ITrackingObserver
    {
        /// <inheritdoc/>
        public TrackingState TrackingState
        {
            get
            {
#if UNITY_WSA
                if (UnityEngine.XR.WSA.WorldManager.state == UnityEngine.XR.WSA.PositionalLocatorState.Active)
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