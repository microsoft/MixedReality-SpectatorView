// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.WSA;
using Stopwatch = System.Diagnostics.Stopwatch;

#if !UNITY_EDITOR && UNITY_WSA
using Windows.Perception;
using Windows.Perception.Spatial;
using Calendar = Windows.Globalization.Calendar;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Component that provides time-adjusted holographic poses to the compositor.
    /// </summary>
    public class CameraPoseProvider : MonoBehaviour
    {
        private INetworkManager networkManager;
        private Stopwatch timestampStopwatch;
        private SpatialCoordinateSystemParticipant sharedCoordinateParticipant;
        private INetworkConnection currentConnection;

#if !UNITY_EDITOR && UNITY_WSA
        private Calendar timeConversionCalendar;
#endif
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
                var connections = networkManager.Connections;
                if (connections.Count > 1)
                {
                    Debug.LogWarning("More than one connection was found, CameraPoseProvider only expects one network connection");
                }

                foreach (var connection in connections)
                {
                    NetworkManagerConnected(connection);
                }
            }
        }

        private void OnDestroy()
        {
            networkManager.Connected -= NetworkManagerConnected;
        }

        private void Update()
        {
            if (currentConnection == null || !currentConnection.IsConnected)
            {
                return;
            }

            if (sharedCoordinateParticipant == null)
            {
                SpatialCoordinateSystemManager.Instance.TryGetSpatialCoordinateSystemParticipant(currentConnection, out sharedCoordinateParticipant);
            }

            // Get an adjusted position and rotation based on the historical pose for the current time.
            // The Unity camera uses pose prediction and doesn't reflect the actual historical pose of
            // the device.
            Vector3 cameraPosition;
            Quaternion cameraRotation;
            bool hasNewPose = GetHistoricalPose(out cameraPosition, out cameraRotation);

            if (hasNewPose)
            {
                float timestamp = (float)timestampStopwatch.Elapsed.TotalSeconds;

                // Translate the camera pose into an anchor-relative pose.
                if (sharedCoordinateParticipant != null && sharedCoordinateParticipant.Coordinate != null)
                {
                    cameraPosition = sharedCoordinateParticipant.Coordinate.WorldToCoordinateSpace(cameraPosition);
                    cameraRotation = sharedCoordinateParticipant.Coordinate.WorldToCoordinateSpace(cameraRotation);
                }

                SendCameraPose(timestamp, cameraPosition, cameraRotation);
            }
        }

        private void NetworkManagerConnected(INetworkConnection connection)
        {
            // Restart the timeline at 0 each time we reconnect to the HoloLens
            timestampStopwatch = Stopwatch.StartNew();
            sharedCoordinateParticipant = null;
            currentConnection = connection;
        }

        private void SendCameraPose(float timestamp, Vector3 cameraPosition, Quaternion cameraRotation)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(stream))
            {
                message.Write(HolographicCameraObserver.CameraCommand);
                message.Write(timestamp);
                message.Write(cameraPosition);
                message.Write(cameraRotation);
                message.Flush();

                networkManager.Broadcast(stream.GetBuffer(), 0, stream.Position);
            }
        }

        private bool GetHistoricalPose(out Vector3 cameraPosition, out Quaternion cameraRotation)
        {
#if !UNITY_EDITOR && UNITY_WSA
            SpatialCoordinateSystem unityCoordinateSystem = Marshal.GetObjectForIUnknown(WorldManager.GetNativeISpatialCoordinateSystemPtr()) as SpatialCoordinateSystem;
            if (unityCoordinateSystem == null)
            {
                Debug.LogError("Failed to get the native SpatialCoordinateSystem");
                cameraPosition = default(Vector3);
                cameraRotation = default(Quaternion);
                return false;
            }

            if (timeConversionCalendar == null)
            {
                timeConversionCalendar = new Calendar();
            }

            timeConversionCalendar.SetToNow();

            PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(timeConversionCalendar.GetDateTime());

            if (perceptionTimestamp != null)
            {
                SpatialLocator locator = SpatialLocator.GetDefault();
                if (locator != null)
                {
                    SpatialLocation headPose = locator.TryLocateAtTimestamp(perceptionTimestamp, unityCoordinateSystem);
                    if (headPose != null)
                    {
                        var systemOrientation = headPose.Orientation;
                        var systemPostion = headPose.Position;

                        // Convert the orientation and position from Windows to Unity coordinate spaces
                        cameraRotation.x = -systemOrientation.X;
                        cameraRotation.y = -systemOrientation.Y;
                        cameraRotation.z = systemOrientation.Z;
                        cameraRotation.w = systemOrientation.W;

                        cameraPosition.x = systemPostion.X;
                        cameraPosition.y = systemPostion.Y;
                        cameraPosition.z = -systemPostion.Z;
                        return true;
                    }
                }
            }

            cameraPosition = default(Vector3);
            cameraRotation = default(Quaternion);
            return false;
#else
            cameraPosition = Camera.main.transform.position;
            cameraRotation = Camera.main.transform.rotation;
            return true;
#endif
        }
    }
}