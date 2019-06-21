using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class QRCodeMarkerVisualSpatialLocalizer : MarkerVisualSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("6CEF83A0-1E40-40DE-B36B-762974EFDBD8");

        public override Guid MarkerVisualDetectorSpatialLocalizerId => DetectorId;
        public static Guid DetectorId => QRCodeMarkerVisualDetectorSpatialLocalizer.Id;
    }
}
