// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public class QRCodeMarkerVisualDetectorSpatialLocalizer : MarkerVisualDetectorSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("95A1F0A8-60D7-49C1-8907-CB7F4D3CF6EB");

        public override string DisplayName => "QR Code Visual Detector";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_WSA && !UNITY_EDITOR
                return (Windows.ApplicationModel.Package.Current.Id.Architecture != Windows.System.ProcessorArchitecture.X86); // HoloLens 1 is not supported.
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
            FieldHelper.ValidateType<QRCodeMarkerDetector>(MarkerDetector);
        }
#endif
    }
}
