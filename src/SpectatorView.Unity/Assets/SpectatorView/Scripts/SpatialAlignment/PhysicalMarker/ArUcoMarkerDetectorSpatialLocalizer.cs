// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ArUcoMarkerDetectorSpatialLocalizer : MarkerDetectorSpatialLocalizer
    {
        public static readonly Guid Id = new Guid("698D46CF-2099-4E06-9ADE-2FD0C18992F4");
        public override Guid SpatialLocalizerId => Id;

        [Tooltip("ArUco marker detector used by the spatial localizer.")]
        [SerializeField]
        private ArUcoMarkerDetector MarkerDetector = null;

        public override string DisplayName => "ArUco Marker";

        protected override bool IsSupported
        {
            get
            {
#if UNITY_EDITOR
                // We return true for the editor so that this localizer registers as available for video camera compositing scenarios.
                return true;
#elif UNITY_WSA
                return Windows.ApplicationModel.Package.Current.Id.Architecture == Windows.System.ProcessorArchitecture.X86;
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
