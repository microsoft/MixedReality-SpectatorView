// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Used for loading/applying camera intrinsics obtained from a single stationary video camera.
    /// </summary>
    public class VideoCameraCalibrationData : CalibrationData
    {
        public VideoCameraCalibrationData(CalculatedCameraIntrinsics cameraIntrinsics)
            : base(cameraIntrinsics, new CalculatedCameraExtrinsics())
        {
        }
    }
}