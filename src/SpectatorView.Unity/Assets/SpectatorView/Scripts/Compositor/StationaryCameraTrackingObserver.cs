// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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