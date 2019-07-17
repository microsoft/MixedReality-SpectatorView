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
    /// <summary>
    /// Provides extension methods that help marshal WinRT IInspectable objects that have been passed to managed code as an IntPtr.
    /// On .NET Native, IInspectable pointers cannot be marshalled from native to managed code using Marshal.GetObjectForIUnknown.
    /// This class calls into a native method that specifically marshals the type as a specific WinRT interface, which
    /// is supported by the marshaller on both .NET Core and .NET Native.
    /// </summary>
    public static class WinRTExtensions
    {
        [DllImport("SpectatorView.WinRTExtensions.dll")]
        public static extern void GetSpatialCoordinateSystem(IntPtr nativePtr, out SpatialCoordinateSystem coordinateSystem);
    }
}
#endif