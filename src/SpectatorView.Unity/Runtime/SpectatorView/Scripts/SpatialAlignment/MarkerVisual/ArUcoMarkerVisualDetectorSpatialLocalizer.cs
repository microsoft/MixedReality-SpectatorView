// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ArUcoMarkerVisualDetectorSpatialLocalizer : MarkerVisualDetectorSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("2DA7D277-323F-4A0D-B3BB-B2BA6D3EF70E");

        public override string DisplayName => "ArUco Marker Visual Detector";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_WSA && !UNITY_EDITOR
                return Windows.ApplicationModel.Package.Current.Id.Architecture == Windows.System.ProcessorArchitecture.X86;
#elif UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FieldHelper.ValidateType<ArUcoMarkerDetector>(MarkerDetector);
        }
#endif
    }
}
