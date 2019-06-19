using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class QRCodeMarkerVisualSpatialLocalizer : MarkerVisualSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => QRCodeMarkerVisualDetectorSpatialLocalizer.Id;

        public override Guid MarkerVisualDetectorSpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("6CEF83A0-1E40-40DE-B36B-762974EFDBD8");

#if UNITY_EDITOR
        private void OnValidate()
        {
            FieldHelper.ValidateType<QRCodeMarkerVisual>(MarkerVisual);
        }
#endif
    }
}
