// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class QRCodeMarkerDetectorSpatialLocalizer : MarkerDetectorSpatialLocalizer
    {
        public static readonly Guid Id = new Guid("22510A62-1957-42BA-9BDA-A77628F06C72");
        public override Guid SpatialLocalizerId => Id;

        [Tooltip("ArUco marker detector used by the spatial localizer.")]
        [SerializeField]
        private QRCodeMarkerDetector MarkerDetector = null;

        public override string DisplayName => "QR Code";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_EDITOR
                // We return true for the editor so that this localizer registers as available for video camera compositing scenarios.
                return true;
#elif QRCODESTRACKER_BINARY_AVAILABLE && UNITY_WSA
                return (global::Windows.ApplicationModel.Package.Current.Id.Architecture != global::Windows.System.ProcessorArchitecture.X86); // HoloLens 1 is not supported.
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            DebugLog("Awake");
            markerDetector = MarkerDetector;
            if (markerDetector == null)
            {
                Debug.LogWarning("Marker detector not appropriately set for MarkerDetectorSpatialLocalizer");
            }
        }
    }
}
