// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class HeadsetRequestHandler : MonoBehaviour
    {
        /// <summary>
        /// Used to receive network messages.
        /// </summary>
        [Tooltip("Used to receive network messages.")]
        [SerializeField]
        private HolographicCameraBroadcaster holographicCameraBroadcaster = null;

        /// <summary>
        /// Used to send network messages.
        /// </summary>
        [Tooltip("Used to send network messages.")]
        [SerializeField]
        private TCPConnectionManager connectionManager = null;

        /// <summary>
        /// Used to obtain marker and headset data.
        /// </summary>
        [Tooltip("Used to obtain marker and headset data.")]
        [SerializeField]
        private HeadsetCalibration headsetCalibration = null;

        private bool initialized = false;
        private string editorAddress = string.Empty;
        private bool updateData = false;
        private HeadsetCalibrationDataRequest request = null;

        private void OnEnable()
        {
            holographicCameraBroadcaster.Disconnected += OnDisconnect;
            holographicCameraBroadcaster.RegisterCommandHandler(HeadsetCalibration.RequestCalibrationDataCommandHeader, CalibrationDataRequested);
            holographicCameraBroadcaster.RegisterCommandHandler(HeadsetCalibration.UploadCalibrationCommandHeader, UploadCalibrationDataAsync);
            headsetCalibration.Updated += OnHeadsetCalibrationUpdated;
        }

        private void EnableChildren()
        {
            if (!initialized)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }

                initialized = true;
            }
        }

        private void DisableChildren()
        {
            if (initialized)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }

                initialized = false;
            }
        }

        private void OnDisconnect(SocketEndpoint endpoint)
        {
            if(endpoint.Address == editorAddress)
            {
                DisableChildren();
            }
        }

        private void CalibrationDataRequested(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            editorAddress = endpoint.Address;
            EnableChildren();

            if (HeadsetCalibrationDataRequest.TryDeserialize(reader, out request))
            {
                Debug.Log("Headset calibration data requested");
                updateData = true;
            }
            else
            {
                Debug.LogWarning("Received network payload that wasn't a headset calibration data request");
                request = null;
            }
        }

#if WINDOWS_UWP
        private async void UploadCalibrationDataAsync(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool succeeded = true;
            string uploadMessage = "Successfully uploaded calibration data.";
            Debug.Log("Received a calibration data payload");
            var size = reader.ReadInt32();
            var data = reader.ReadBytes(size);
            if (CalculatedCameraCalibration.TryDeserialize(data, out var calibrationData))
            {
                var fileName = "CalibrationData.json";
                Windows.Storage.StorageFile file = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteBytesAsync(file, data);
            }
            else
            {
                succeeded = false;
                uploadMessage = "Uploading calibration data failed -  failed to deserialize calibration data.";
                Debug.LogError(uploadMessage);
            }

            SendUploadResult(succeeded, uploadMessage);
        }
#else
        private void UploadCalibrationDataAsync(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            SendUploadResult(false, "Uploading calibration data not supported for current platform.");
        }
#endif

        private void Update()
        {
            if (updateData)
            {
                updateData = false;

                if (headsetCalibration == null)
                {
                    Debug.LogWarning("HeadsetCalibration field is not set, unable to create headset calibration data payload");
                    return;
                }

                headsetCalibration.UpdateHeadsetCalibrationData();
            }
        }

        private void OnHeadsetCalibrationUpdated(HeadsetCalibrationData data)
        {
            if (holographicCameraBroadcaster != null)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(memoryStream))
                    {
                        writer.Write(HeadsetCalibration.CalibrationDataReceivedCommandHeader);
                        data.SerializeAndWrite(writer);
                        writer.Flush();

                        Debug.Log("Sending headset calibration data payload.");
                        connectionManager.Broadcast(memoryStream.ToArray());
                    }
                }
            }
        }

        private void SendUploadResult(bool succeeded, string uploadMessage)
        {
            if (holographicCameraBroadcaster != null)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(memoryStream))
                    {
                        writer.Write(HeadsetCalibration.UploadCalibrationResultCommandHeader);
                        writer.Write(succeeded);
                        writer.Write(uploadMessage);
                        writer.Flush();

                        Debug.Log("Sending upload result message.");
                        connectionManager.Broadcast(memoryStream.ToArray());
                    }
                }
            }
        }
    }
}
