// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Used for loading/applying camera intrinsics and camera extrinsics obtained through Spectator View's default calibration process for a
    /// camera attached to a head-mounted display.
    /// </summary>
    public class HeadsetRigCalibrationData : CalibrationData
    {
        public HeadsetRigCalibrationData(string cameraIntrinsicsPath, string cameraExtrinsicsPath)
            : base(cameraIntrinsicsPath, cameraExtrinsicsPath)
        {
        }

        public HeadsetRigCalibrationData(CalculatedCameraIntrinsics intrinsics, CalculatedCameraExtrinsics extrinsics)
            : base(intrinsics, extrinsics)
        {
        }

        /// <inheritdoc />
        public override void SetUnityCameraExtrinstics(Transform cameraTransform)
        {
            base.SetUnityCameraExtrinstics(cameraTransform);

            // Magic offset from Unity's underlying coordinate frame (WorldManager.GetNativeISpatialCoordinateSystemPtr()) and the head pose used for the camera.
            // Poses are sent in the coordinate frame space because the Unity camera position uses prediction.
            cameraTransform.localPosition += new Vector3(0f, 0.08f, 0.08f);
        }
    }
}
