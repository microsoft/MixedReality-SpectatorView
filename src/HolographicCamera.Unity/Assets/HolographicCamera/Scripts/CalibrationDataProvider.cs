// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_WSA
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Loads calibration data from the Pictures library on the device and transfers that data
    /// to the compositor upon connection.
    /// </summary>
    public class CalibrationDataProvider : MonoBehaviour
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
                SendCalibrationDataAsync();
            }
        }

        private void OnDestroy()
        {
            networkManager.Connected -= NetworkManagerConnected;
        }

        private void NetworkManagerConnected(INetworkConnection obj)
        {
            SendCalibrationDataAsync();
        }

#if !UNITY_EDITOR && UNITY_WSA
        private async void SendCalibrationDataAsync()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                StorageFile file = (await KnownFolders.PicturesLibrary.TryGetItemAsync(@"CalibrationData.json").AsTask()) as StorageFile;
                if (file != null)
                {
                    byte[] contents = (await FileIO.ReadBufferAsync(file)).ToArray();
                    if (CalculatedCameraCalibration.TryDeserialize(contents, out CalculatedCameraCalibration calibration))
                    {
                        // Magic offset from Unity's underlying coordinate frame (WorldManager.GetNativeISpatialCoordinateSystemPtr()) and the head pose used for the camera.
                        // Poses are sent in the coordinate frame space because the Unity camera position uses prediction.
                        Matrix4x4 viewFromWorld = calibration.Extrinsics.ViewFromWorld;
                        Vector3 position = viewFromWorld.GetColumn(3);
                        position += new Vector3(0f, 0.08f, 0.08f);
                        viewFromWorld.SetColumn(3, position);

                        calibration.Extrinsics.ViewFromWorld = viewFromWorld;
                        contents = calibration.Serialize();
                    }

                    message.Write("CalibrationData");
                    message.Write(contents.Length);
                    message.Write(contents);
                    networkManager.Broadcast(memoryStream.GetBuffer(), 0, memoryStream.Position);
                }
            }
        }
#else
        private void SendCalibrationDataAsync()
        {
        }
#endif
    }
}