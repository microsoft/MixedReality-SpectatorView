using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class ArUcoMarkerVisualSpatialLocalizer : MarkerVisualSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("BA5C8EA7-439C-4E1A-9925-218A391EF309");

        public override Guid MarkerVisualDetectorSpatialLocalizerId => DetectorId;
        public static Guid DetectorId => ArUcoMarkerVisualDetectorSpatialLocalizer.Id;
    }
}
