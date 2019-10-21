// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ArUcoMarkerVisualSpatialLocalizer : MarkerVisualSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("BA5C8EA7-439C-4E1A-9925-218A391EF309");

        public override string DisplayName => "ArUco Marker Visual";

        public override Guid MarkerVisualDetectorSpatialLocalizerId => DetectorId;
        public static Guid DetectorId => ArUcoMarkerVisualDetectorSpatialLocalizer.Id;

        protected override bool IsSupported
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
