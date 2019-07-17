// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_WSA && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Windows.Perception.Spatial;

namespace Microsoft.MixedReality.PhotoCapture
{
    public static class WinRTExtensions
    {
        [DllImport("SpectatorView.WinRTExtensions.dll")]
        public static extern void GetSpatialCoordinateSystem(IntPtr nativePtr, out SpatialCoordinateSystem coordinateSystem);
    }
}
#endif