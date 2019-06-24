// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public class QRCodeMarkerVisualSpatialLocalizer : MarkerVisualSpatialLocalizer
    {
        public override Guid SpatialLocalizerId => Id;
        public static readonly Guid Id = new Guid("6CEF83A0-1E40-40DE-B36B-762974EFDBD8");

        public override Guid MarkerVisualDetectorSpatialLocalizerId => DetectorId;
        public static Guid DetectorId => QRCodeMarkerVisualDetectorSpatialLocalizer.Id;
    }
}
