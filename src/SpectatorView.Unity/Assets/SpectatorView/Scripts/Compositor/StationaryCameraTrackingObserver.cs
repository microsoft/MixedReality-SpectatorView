using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class StationaryCameraTrackingObserver : TrackingObserver
    {
        public override TrackingState TrackingState => TrackingState.Tracking;
    }
}