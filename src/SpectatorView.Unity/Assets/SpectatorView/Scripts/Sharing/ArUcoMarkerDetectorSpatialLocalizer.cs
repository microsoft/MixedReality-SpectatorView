// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.MarkerDetection;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView
{
    public class ArUcoMarkerDetectorSpatialLocalizer : MarkerDetectorSpatialLocalizer
    {
        public static readonly Guid Id = new Guid("698D46CF-2099-4E06-9ADE-2FD0C18992F4");
        public override Guid SpatialLocalizerId => Id;

        [Tooltip("ArUco marker detector used by the spatial localizer.")]
        [SerializeField]
        private SpectatorViewPluginArUcoMarkerDetector MarkerDetector = null;

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
