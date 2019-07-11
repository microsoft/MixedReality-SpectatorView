// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    public class HeadsetCalibrationData
    {
        public float timestamp;
        public HeadsetData headsetData;
        public List<MarkerPair> markers;

        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        public void SerializeAndWrite(BinaryWriter writer)
        {
            var str = JsonUtility.ToJson(this);
            writer.Write(str);
        }

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

    [Serializable]
    public struct MarkerPair
    {
        public int id;
        public MarkerCorners qrCodeMarkerCorners;
        public MarkerCorners arucoMarkerCorners;
    }

    [Serializable]
    public struct MarkerCorners
    {
        public Vector3 topLeft;
        public Vector3 topRight;
        public Vector3 bottomLeft;
        public Vector3 bottomRight;
        public Quaternion orientation;
    }

    [Serializable]
    public struct HeadsetData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public class HeadsetCalibrationDataRequest
    {
        public float timestamp;

        public byte[] Serialize()
        {
            var str = JsonUtility.ToJson(this);
            var payload = Encoding.UTF8.GetBytes(str);
            return payload;
        }

        public void SerializeAndWrite(BinaryWriter writer)
        {
            var str = JsonUtility.ToJson(this);
            writer.Write(str);
        }

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
