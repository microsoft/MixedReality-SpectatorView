// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Component responsible for requesting localization of shared coordinates on devices
    /// connected to the compositor. Shared coordinates are persisted using
    /// the <see cref="WorldAnchorSpatialLocalizer"/> after they are resolved, and
    /// are restored upon initial connection.
    /// </summary>
    public class CompositorWorldAnchorLocalizationManager : Singleton<CompositorWorldAnchorLocalizationManager>
    {
        private const string CompositorWorldAnchorId = "Compositor_SharedSpatialCoordinate";

        private readonly Dictionary<SpatialCoordinateSystemParticipant, Task<bool>> participantLocalizationTasks = new Dictionary<SpatialCoordinateSystemParticipant, Task<bool>>();

        private void Start()
        {
            SpatialCoordinateSystemManager.Instance.ParticipantConnected += OnParticipantConnected;
            SpatialCoordinateSystemManager.Instance.ParticipantDisconnected += OnParticipantDisconnected;
        }

        protected override void OnDestroy()
        {
            SpatialCoordinateSystemManager.Instance.ParticipantConnected -= OnParticipantConnected;
            SpatialCoordinateSystemManager.Instance.ParticipantDisconnected -= OnParticipantDisconnected;
        }

        private void OnParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            // When a new participant connects, send a request to re-load the shared spatial coordinate world anchor.
            participantLocalizationTasks.Add(participant, SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.NetworkConnection, WorldAnchorSpatialLocalizer.Id, new WorldAnchorSpatialLocalizationSettings
            {
                Mode = WorldAnchorLocalizationMode.LocateExistingAnchor,
                AnchorId = CompositorWorldAnchorId
            }));
        }

        private void OnParticipantDisconnected(SpatialCoordinateSystemParticipant participant)
        {
            participantLocalizationTasks.Remove(participant);
        }

        /// <summary>
        /// Runs a localization session on the specific <see cref="SpatialCoordinateSystemParticipant"/>'s connected peer, followed
        /// by creating a persisted WorldAnchor-based <see cref="Microsoft.MixedReality.SpatialAlignment.ISpatialCoordinate"/> based on the located coordinate.
        /// </summary>
        /// <param name="participant">The participant to use to initiate the remote localization sessions.</param>
        /// <param name="spatialLocalizerId">The ID of the <see cref="Microsoft.MixedReality.SpectatorView.ISpatialLocalizer"/> to use
        /// for discovering a spatial coordinate.</param>
        /// <param name="settings">The settings to pass to the remote localizer.</param>
        public async void RunRemoteLocalizationWithWorldAnchorPersistence(SpatialCoordinateSystemParticipant participant, Guid spatialLocalizerId, ISpatialLocalizationSettings settings)
        {
            // If the initial request to restore a coordinate from a WorldAnchor hasn't completed, wait for that to complete first.
            if (participantLocalizationTasks.TryGetValue(participant, out Task<bool> currentTask))
            {
                await currentTask;
            }

            // Request localization using the specific localizer and settings, and wait for that localization to complete.
            participantLocalizationTasks[participant] = currentTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.NetworkConnection, spatialLocalizerId, settings);
            bool localizationSucceeded = await currentTask;

            if (localizationSucceeded)
            {
                // Once the specific localizer has found a shared coordinate, ask the WorldAnchorSpatialLocalizer
                // to create a WorldAnchor-based coordinate at the same location, and persist that coordinate across sessions.
                participantLocalizationTasks[participant] = currentTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.NetworkConnection, WorldAnchorSpatialLocalizer.Id, new WorldAnchorSpatialLocalizationSettings
                {
                    Mode = WorldAnchorLocalizationMode.CreateAnchorAtWorldTransform,
                    AnchorId = CompositorWorldAnchorId,
                    AnchorPosition = participant.PeerSpatialCoordinateWorldPosition,
                    AnchorRotation = participant.PeerSpatialCoordinateWorldRotation
                });
                await currentTask;
            }
            else
            {
                Debug.LogError($"Remote localization failed on device {participant.NetworkConnection} for spatial localizer {spatialLocalizerId}");
            }
        }
    }
}