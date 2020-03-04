// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class StationaryCameraCalibrationDataProvider : MonoBehaviour
    {
        private INetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<INetworkManager>();
            if (networkManager == null)
            {
                throw new MissingComponentException("Missing network manager component");
            }

            networkManager.Connected += NetworkManagerConnected;
            if (networkManager.IsConnected)
            {
                SendCalibrationData();
            }
        }

        private void OnDestroy()
        {
            networkManager.Connected -= NetworkManagerConnected;
        }

        private void NetworkManagerConnected(INetworkConnection obj)
        {
            SendCalibrationData();
        }

#if UNITY_EDITOR && UNITY_WSA
        private void SendCalibrationData()
        {
            UnityCompositorInterface.GetCameraCalibrationInformation(out CompositorCameraIntrinsics compositorIntrinsics);
            CalculatedCameraCalibration calibration = new CalculatedCameraCalibration(compositorIntrinsics.AsCalculatedCameraIntrinsics(), new CalculatedCameraExtrinsics());
            byte[] serializedCalibration = calibration.Serialize();

            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                message.Write("CalibrationData");
                message.Write(serializedCalibration.Length);
                message.Write(serializedCalibration);
                var packet = memoryStream.ToArray();
                networkManager.Broadcast(packet, 0, packet.LongLength);
            }
        }
#else
        private void SendCalibrationData()
        {
        }
#endif
    }
}