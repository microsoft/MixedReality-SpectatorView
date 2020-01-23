using Microsoft.MixedReality.SpatialAlignment;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Microsoft.MixedReality.SpectatorView
{
    public class StationaryCameraPoseProvider : MonoBehaviour
    {
        private INetworkManager networkManager;
        private Stopwatch timestampStopwatch;
        private SpatialCoordinateSystemParticipant sharedCoordinateParticipant;
        private INetworkConnection currentConnection;

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

            float timestamp = (float)timestampStopwatch.Elapsed.TotalSeconds;

            Vector3 cameraPosition = Vector3.zero;
            Quaternion cameraRotation = Quaternion.identity;

            // Translate the camera pose into an anchor-relative pose.
            if (sharedCoordinateParticipant != null && sharedCoordinateParticipant.Coordinate != null)
            {
                cameraPosition = sharedCoordinateParticipant.Coordinate.WorldToCoordinateSpace(cameraPosition);
                cameraRotation = sharedCoordinateParticipant.Coordinate.WorldToCoordinateSpace(cameraRotation);
            }

            SendCameraPose(timestamp, cameraPosition, cameraRotation);
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
    }
}