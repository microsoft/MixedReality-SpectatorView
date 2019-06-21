using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Utilities;
using System;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class ArUcoMarkerVisualDetectorSpatialLocalizer : MarkerVisualDetectorSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("2DA7D277-323F-4A0D-B3BB-B2BA6D3EF70E");

#if UNITY_EDITOR
        private void OnValidate()
        {
            FieldHelper.ValidateType<SpectatorViewPluginArUcoMarkerDetector>(MarkerDetector);
        }
#endif
    }
}
