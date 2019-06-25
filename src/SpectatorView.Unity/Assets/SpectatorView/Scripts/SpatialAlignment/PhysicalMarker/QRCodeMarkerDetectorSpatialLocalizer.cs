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
