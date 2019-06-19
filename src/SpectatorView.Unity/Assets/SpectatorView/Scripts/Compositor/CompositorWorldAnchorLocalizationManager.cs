using Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.WorldAnchors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.Compositor
{
    public class CompositorWorldAnchorLocalizationManager : Singleton<CompositorWorldAnchorLocalizationManager>
    {
        private const string CompositorWorldAnchorId = "Compositor_SharedSpatialCoordinate";

        private readonly Dictionary<SpatialCoordinateSystemParticipant, Task> participantLocalizationTasks = new Dictionary<SpatialCoordinateSystemParticipant, Task>();

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
            // When a new participant connects, send a request to re-load the shared spatial coordinate world anchor
            participantLocalizationTasks.Add(participant, SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, WorldAnchorSpatialLocalizer.Id, new WorldAnchorSpatialLocalizationSettings
            {
                Mode = WorldAnchorLocalizationMode.LocateExistingAnchor,
                AnchorId = CompositorWorldAnchorId
            }));
        }

        private void OnParticipantDisconnected(SpatialCoordinateSystemParticipant participant)
        {
            participantLocalizationTasks.Remove(participant);
        }

        public async void RunRemoteLocalizationWithWorldAnchorPersistence(SpatialCoordinateSystemParticipant participant, Guid spatialLocalizerId, ISpatialLocalizationSettings settings)
        {
            if (participantLocalizationTasks.TryGetValue(participant, out Task currentTask))
            {
                await currentTask;
            }

            participantLocalizationTasks[participant] = currentTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, spatialLocalizerId, settings);
            await currentTask;

            participantLocalizationTasks[participant] = currentTask = SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, WorldAnchorSpatialLocalizer.Id, new WorldAnchorSpatialLocalizationSettings
            {
                Mode = WorldAnchorLocalizationMode.CreateAnchorAtWorldTransform,
                AnchorId = CompositorWorldAnchorId,
                AnchorPosition = participant.PeerSpatialCoordinateWorldPosition,
                AnchorRotation = participant.PeerSpatialCoordinateWorldRotation
            });
            await currentTask;
        }
    }
}