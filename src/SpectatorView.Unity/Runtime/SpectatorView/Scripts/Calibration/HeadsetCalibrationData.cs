// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Data that the HoloLens device sends to the editor during calibration.
    /// </summary>
    [Serializable]
    public class HeadsetCalibrationData
    {
        /// <summary>
        /// The HoloLens device's application time when this data was created.
        /// </summary>
        public float timestamp;

        /// <summary>
        /// The headset world position and orientation.
        /// </summary>
        public HeadsetData headsetData;

        /// <summary>
        /// QR Code and ArUco marker locations in world space.
        /// </summary>
        public List<MarkerPair> markers;

        /// <summary>
        /// Call to serialize class contents into a byte array for sending over a network.
        /// </summary>
        /// <returns>byte array payload</returns>
        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        /// <summary>
        /// Call to serialize and write a byte array to the provided binary writer.
        /// </summary>
        /// <param name="writer">Writer that serialized data is written to</param>
        public void SerializeAndWrite(BinaryWriter writer)
        {
            var str = JsonUtility.ToJson(this);
            writer.Write(str);
        }

        /// <summary>
        /// Called to try and create a HeadsetCalibrationData instance from a byte array.
        /// </summary>
        /// <param name="payload">byte array to deserialize</param>
        /// <param name="headsetCalibrationData">output headset calibration data</param>
        /// <returns>Returns true if the provided payload could be converted into headset calibration data, otherwise false.</returns>
        public static bool TryDeserialize(byte[] payload, out HeadsetCalibrationData headsetCalibrationData)
        {
            headsetCalibrationData = null;

            try
            {
                var str = Encoding.UTF8.GetString(payload);
                headsetCalibrationData = JsonUtility.FromJson<HeadsetCalibrationData>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown: {e}");
                return false;
            }
        }

        /// <summary>
        /// Call to try and create a HeadsetCalibrationData instance from a binary reader.
        /// </summary>
        /// <param name="reader">Binary reader to obtain serialized data from</param>
        /// <param name="headsetCalibrationData">output headset calibration data</param>
        /// <returns>Returns true if the provided binary reader could be used to create headset calibration data, otherwise false.</returns>
        public static bool TryDeserialize(BinaryReader reader, out HeadsetCalibrationData headsetCalibrationData)
        {
            headsetCalibrationData = null;
            var str = reader.ReadString();
            try
            {
                headsetCalibrationData = JsonUtility.FromJson<HeadsetCalibrationData>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown: {e}");
                return false;
            }
        }
    }

    /// <summary>
    /// Struct that contains locations and orientations for QR Code and ArUco markers that have the same id.
    /// </summary>
    [Serializable]
    public struct MarkerPair
    {
        public int id;
        public MarkerCorners qrCodeMarkerCorners;
        public MarkerCorners arucoMarkerCorners;
    }

    /// <summary>
    /// Struct that contains world positions and orientations for marker corners.
    /// </summary>
    [Serializable]
    public struct MarkerCorners
    {
        public Vector3 topLeft;
        public Vector3 topRight;
        public Vector3 bottomLeft;
        public Vector3 bottomRight;
        public Quaternion orientation;
    }

    /// <summary>
    /// Struct that contains the world position and orientation of a HoloLens device.
    /// </summary>
    [Serializable]
    public struct HeadsetData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// Data that the editor sends to a HoloLens device during calibration
    /// </summary>
    [Serializable]
    public class HeadsetCalibrationDataRequest
    {
        /// <summary>
        /// The editor application time when this request was created.
        /// </summary>
        public float timestamp;

        /// <summary>
        /// Call to serialize class data into a byte array payload for sending over a network.
        /// </summary>
        /// <returns>byte array payload</returns>
        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        /// <summary>
        /// Call to serialize class data into a byte array that is then written to the provided writer.
        /// </summary>
        /// <param name="writer">binary writer where serialized data is written</param>
        public void SerializeAndWrite(BinaryWriter writer)
        {
            var str = JsonUtility.ToJson(this);
            writer.Write(str);
        }

        /// <summary>
        /// Call to try and deserialize a byte array paylod to create a headset calibration data request.
        /// </summary>
        /// <param name="payload">byte array to deserialize</param>
        /// <param name="request">output headset calibration data request</param>
        /// <returns>Returns true if the payload was successfully converted to a headset calibration data request, otherwise false.</returns>
        public static bool TryDeserialize(byte[] payload, out HeadsetCalibrationDataRequest request)
        {
            request = null;
            try
            {
                var str = Encoding.UTF8.GetString(payload);
                request = JsonUtility.FromJson<HeadsetCalibrationDataRequest>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown: {e}");
                return false;
            }
        }

        /// <summary>
        /// Call to read and deserialize a byte array paylod to create a headset calibration data request.
        /// </summary>
        /// <param name="reader">reader for obtaining the byte array to deserialize</param>
        /// <param name="request">output headset calibration data request</param>
        /// <returns>Returns true if the payload was successfully converted to a headset calibration data request, otherwise false.</returns>
        public static bool TryDeserialize(BinaryReader reader, out HeadsetCalibrationDataRequest request)
        {
            request = null;
            var str = reader.ReadString();
            try
            {
                request = JsonUtility.FromJson<HeadsetCalibrationDataRequest>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown: {e}");
                return false;
            }
        }
    }
}
