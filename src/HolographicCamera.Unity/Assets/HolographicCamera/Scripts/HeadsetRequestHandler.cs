// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class HeadsetRequestHandler : MonoBehaviour
    {
        /// <summary>
        /// Used to send and receive network messages.
        /// </summary>
        [Tooltip("Used to send and receive network messages.")]
        [SerializeField]
        protected HolographicCameraBroadcaster holographicCameraBroadcaster;

        [SerializeField]
        private TCPConnectionManager connectionManager = null;

        /// <summary>
        /// Used to obtain marker and headset data.
        /// </summary>
        [Tooltip("Used to obtain marker and headset data.")]
        [SerializeField]
        protected HeadsetCalibration headsetCalibration;

        /// <summary>
        /// Used to display the last request timestamp.
        /// </summary>
        [Tooltip("Used to display the last request timestamp.")]
        [SerializeField]
        protected Text lastRequestTimestampText;

        private bool initialized = false;
        private string editorAddress = string.Empty;
        private bool updateData = false;
        private HeadsetCalibrationDataRequest request = null;

        private void OnEnable()
        {
            holographicCameraBroadcaster.Disconnected += OnDisconnect;
            holographicCameraBroadcaster.RegisterCommandHandler(HeadsetCalibration.RequestCalibrationDataCommandHeader, CalibrationDataRequested);
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

                if (request != null &&
                    lastRequestTimestampText != null)
                {
                    lastRequestTimestampText.text = $"Last Request Timestamp: {request.timestamp}";
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

                        Debug.Log("Sending headset calibration data payload");
                        connectionManager.Broadcast(memoryStream.ToArray());
                    }
                }
            }
        }
    }
}
