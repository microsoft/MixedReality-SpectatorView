using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class QRCodeMarkerVisualDetectorSpatialLocalizer : MarkerVisualDetectorSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("95A1F0A8-60D7-49C1-8907-CB7F4D3CF6EB");

#if UNITY_EDITOR
        private void OnValidate()
        {
            FieldHelper.ValidateType<QRCodeMarkerDetector>(MarkerDetector);
        }
#endif
    }
}
