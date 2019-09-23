// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for an ARKit device.
    /// </summary>
    public class ARKitTrackingObserver : MonoBehaviour, ITrackingObserver
    {
#pragma warning disable 414
        private TrackingState trackingState = TrackingState.Unknown;
#pragma warning restore 414

#if UNITY_IOS
        private void Start()
        {
            UnityEngine.XR.iOS.UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += OnTrackingChangedEvent;
        }

        private void OnTrackingChangedEvent(UnityEngine.XR.iOS.UnityARCamera camera)
        {
            switch (camera.trackingState)
            {
                case UnityEngine.XR.iOS.ARTrackingState.ARTrackingStateNormal:
                    trackingState = TrackingState.Tracking;
                    break;
                case UnityEngine.XR.iOS.ARTrackingState.ARTrackingStateLimited:
                    trackingState = TrackingState.LostTracking;
                    break;
                case UnityEngine.XR.iOS.ARTrackingState.ARTrackingStateNotAvailable:
                    trackingState = TrackingState.Unknown;
                    break;
            }
        }
#endif

        /// <inheritdoc/>
        public TrackingState TrackingState
        {
            get
            {
#if UNITY_IOS
                return trackingState;
#elif UNITY_EDITOR
                return TrackingState.Tracking;
#else
                return TrackingState.Unknown;
#endif
            }
        }
    }
}