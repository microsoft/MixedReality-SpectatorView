// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public abstract class TrackingObserver : MonoBehaviour,
        ITrackingObserver
    {
        /// <inheritdoc/>
        public virtual TrackingState TrackingState => throw new NotImplementedException();

        public void Start()
        {
            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.RegisterTrackingObserver(this);
            }
        }

        public void OnDestroy()
        {
            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.UnregisterTrackingObserver(this);
            }
        }
    }
}
