// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Positions a transform representing a world origin such that a connected peer device's world origin (relative to a
    /// shared spatial coordinate) is used as the effective local world origin (as determined
    /// by the shared spatial coordinate).
    /// </summary>
    public class SpatialCoordinateTransformer : Singleton<SpatialCoordinateTransformer>
    {
        [SerializeField]
        private bool debugLogging = false;

        [Tooltip("The transform that should be translated to the position of the world origin of the peer device")]
        [SerializeField]
        private Transform sharedCoordinateOrigin = null;

        public Transform SharedCoordinateOrigin => sharedCoordinateOrigin;

        private SpatialCoordinateSystemParticipant currentParticipant;

        private void Start()
        {
            DebugLog("Registering ParticipantConnected and ParticipantDisconnected events.");
            SpatialCoordinateSystemManager.Instance.ParticipantConnected += OnParticipantConnected;
            SpatialCoordinateSystemManager.Instance.ParticipantDisconnected += OnParticipantDisconnected;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DebugLog("Unregistering ParticipantConnected and ParticipantDisconnected events.");
            SpatialCoordinateSystemManager.Instance.ParticipantConnected -= OnParticipantConnected;
            SpatialCoordinateSystemManager.Instance.ParticipantDisconnected -= OnParticipantDisconnected;
        }

        private void Update()
        {
            if (currentParticipant != null && sharedCoordinateOrigin != null && currentParticipant.Coordinate != null && currentParticipant.PeerSpatialCoordinateIsLocated)
            {
                // Obtain a position and rotation that transforms this application's local world origin to the shared spatial coordinate space.
                var localWorldToCoordinatePosition = currentParticipant.Coordinate.WorldToCoordinateSpace(Vector3.zero);
                var localWorldToCoordinateRotation = currentParticipant.Coordinate.WorldToCoordinateSpace(Quaternion.identity);

                // Obtain a position and rotation that transforms the peer's shared spatial coordinate to its local world space.
                var peerCoordinateToWorldPosition = currentParticipant.PeerSpatialCoordinateWorldPosition;
                var peerCoordinateToWorldRotation = currentParticipant.PeerSpatialCoordinateWorldRotation;

                // Create a transform that converts the local world space to the peer world space (peer coordinate to peer world * local world to local shared coordinate).
                var matrix = Matrix4x4.TRS(peerCoordinateToWorldPosition, peerCoordinateToWorldRotation, Vector3.one) * Matrix4x4.TRS(localWorldToCoordinatePosition, localWorldToCoordinateRotation, Vector3.one);
                Vector3 position = matrix.GetColumn(3);
                var rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));

                if (sharedCoordinateOrigin.position != position ||
                    sharedCoordinateOrigin.rotation != rotation)
                {
                    DebugLog($"World To Coordinate, Position:{localWorldToCoordinatePosition.ToString("G4")}, Rotation:{localWorldToCoordinateRotation.ToString("G4")}");
                    DebugLog($"Peer Coordinate To World, Position:{peerCoordinateToWorldPosition.ToString("G4")}, Rotation:{peerCoordinateToWorldRotation.ToString("G4")}");

                    sharedCoordinateOrigin.rotation = rotation;
                    sharedCoordinateOrigin.position = position;
                    DebugLog($"Updated transform, Position: {sharedCoordinateOrigin.position.ToString("G4")}, Rotation: {sharedCoordinateOrigin.rotation.ToString("G4")}");
                }
            }
        }

        private void OnParticipantDisconnected(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Participant disconnected: {participant?.NetworkConnection?.ToString() ?? "Unknown NetworkConnection"}");
            if (currentParticipant == null)
            {
                DebugLog("No participant was registered when a participant disconnected");
            }
            currentParticipant = null;
        }

        private void OnParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Participant connected: {participant?.NetworkConnection?.ToString() ?? "Unknown NetworkConnection"}");
            if (currentParticipant != null)
            {
                DebugLog("Participant was already registered when new participant connected");
            }
            currentParticipant = participant;
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"SpatialCoordinateWorldOriginTransformer: {message}");
            }
        }
    }
}