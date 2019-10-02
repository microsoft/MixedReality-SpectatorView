// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// MonoBehaviour that reports tracking information for an ARKit device.
    /// </summary>
    public class ARKitTrackingObserver : TrackingObserver
    {
#pragma warning disable 414
        private TrackingState trackingState = TrackingState.Unknown;
#pragma warning restore 414

#if UNITY_IOS
        protected override void Start()
        {
            base.Start();
            UnityEngine.XR.iOS.UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += OnTrackingChangedEvent;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnityEngine.XR.iOS.UnityARSessionNativeInterface.ARSessionTrackingChangedEvent -= OnTrackingChangedEvent;
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
                default:
                    trackingState = TrackingState.Unknown;
                    break;
            }
        }
#endif

        /// <inheritdoc/>
        public override TrackingState TrackingState
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