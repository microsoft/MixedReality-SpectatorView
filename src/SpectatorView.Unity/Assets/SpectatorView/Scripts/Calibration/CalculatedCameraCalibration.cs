// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// A class containing camera intrinsic and extrinsic information produced through calibration.
    /// </summary>
    [Serializable]
    public class CalculatedCameraCalibration
    {
        /// <summary>
        /// Camera extrinsics calculated through calibration.
        /// </summary>
        public CalculatedCameraExtrinsics Extrinsics;

        /// <summary>
        /// Camera intrinsics calculated through calibration.
        /// </summary>
        public CalculatedCameraIntrinsics Intrinsics;

        public CalculatedCameraCalibration() {}

        public CalculatedCameraCalibration(CalculatedCameraIntrinsics intrinsics, CalculatedCameraExtrinsics extrinsics)
        {
            Intrinsics = intrinsics;
            Extrinsics = extrinsics;
        }

        /// <summary>
        /// Generates a byte payload for the class.
        /// </summary>
        /// <returns>byte payload</returns>
        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        /// <summary>
        /// Attempts to create a CalculatedCameraCalibration given a byte payload.
        /// </summary>
        /// <param name="payload">input byte payload</param>
        /// <param name="calibrationData">output calibration data</param>
        /// <returns>Returns true if the payload was successfully used to generate calibration data, otherwise false</returns>
        public static bool TryDeserialize(byte[] payload, out CalculatedCameraCalibration calibrationData)
        {
            calibrationData = null;

            try
            {
                var str = Encoding.UTF8.GetString(payload);
                calibrationData = JsonUtility.FromJson<CalculatedCameraCalibration>(str);
                return calibrationData.Extrinsics != null &&
                       calibrationData.Intrinsics != null &&
                       calibrationData.Intrinsics.ImageWidth > 0 &&
                       calibrationData.Intrinsics.ImageHeight > 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception thrown deserializing camera intrinsics: {e.ToString()}");
                return false;
            }
        }
    }
}