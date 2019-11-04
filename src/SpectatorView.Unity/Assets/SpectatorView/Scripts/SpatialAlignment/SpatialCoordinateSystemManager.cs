// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialCoordinateSystemManager : Singleton<SpatialCoordinateSystemManager>
    {
        /// <summary>
        /// Check for debug logging.
        /// </summary>
        [Tooltip("Check for debug logging.")]
        [SerializeField]
        private bool debugLogging = false;

        /// <summary>
        /// Check to show debug visuals.
        /// </summary>
        [Tooltip("Check to show debug visuals.")]
        public bool showDebugVisuals = false;

        /// <summary>
        /// Game Object to render at spatial coordinate locations when showing debug visuals.
        /// </summary>
        [Tooltip("Game Object to render at spatial coordinate locations when showing debug visuals.")]
        public GameObject debugVisual = null;

        /// <summary>
        /// Debug visual scale.
        /// </summary>
        [Tooltip("Debug visual scale.")]
        public float debugVisualScale = 1.0f;

        public event Action<SpatialCoordinateSystemParticipant> ParticipantConnected;

        public event Action<SpatialCoordinateSystemParticipant> ParticipantDisconnected;

        internal const string CoordinateStateMessageHeader = "COORDSTATE";
        internal const string SupportedLocalizersMessageHeader = "SUPPORTLOC";
        private const string LocalizeCommand = "LOCALIZE";
        private const string LocalizationCompleteCommand = "LOCALIZEDONE";
        private readonly Dictionary<Guid, ISpatialLocalizer> localizers = new Dictionary<Guid, ISpatialLocalizer>();
        private readonly Dictionary<INetworkConnection, TaskCompletionSource<bool>> remoteLocalizationSessions = new Dictionary<INetworkConnection, TaskCompletionSource<bool>>();
        private Dictionary<INetworkConnection, SpatialCoordinateSystemParticipant> participants = new Dictionary<INetworkConnection, SpatialCoordinateSystemParticipant>();
        private HashSet<INetworkManager> networkManagers = new HashSet<INetworkManager>();
        private ISpatialLocalizationSession currentLocalizationSession = null;
        private ITrackingObserver trackingObserver = null;

        /// <summary>
        /// Current Tracking state for the AR/VR Device associated with the application.
        /// </summary>
        public TrackingState TrackingState
        {
            get
            {
                if (trackingObserver == null)
                {
                    return TrackingState.Unknown;
                }

                return trackingObserver.TrackingState;
            }
        }

        /// <summary>
        /// True if all local and peer coordinates known to the device have been found in the shared experience space.
        /// </summary>
        public bool AllCoordinatesLocated
        {
            get
            {
                bool allFound = participants.Count > 0;
                foreach (var participantPair in participants)
                {
                    if (participantPair.Value.Coordinate == null ||
                        participantPair.Value.IsLocatingSpatialCoordinate ||
                        !participantPair.Value.PeerSpatialCoordinateIsLocated)
                    {
                        allFound = false;
                        break;
                    }
                }

                return allFound;
            }
        }

        public IReadOnlyCollection<ISpatialLocalizer> Localizers
        {
            get
            {
                return localizers.Values;
            }
        }

        public void RegisterSpatialLocalizer(ISpatialLocalizer localizer)
        {
            if (localizers.ContainsKey(localizer.SpatialLocalizerId))
            {
                Debug.LogError($"Cannot register multiple SpatialLocalizers with the same ID {localizer.SpatialLocalizerId}");
                return;
            }

            DebugLog($"Registering spatial localizer: {localizer.SpatialLocalizerId}");
            localizers.Add(localizer.SpatialLocalizerId, localizer);
        }

        public void UnregisterSpatialLocalizer(ISpatialLocalizer localizer)
        {
            if (!localizers.Remove(localizer.SpatialLocalizerId))
            {
                Debug.LogError($"Attempted to unregister SpatialLocalizer with ID {localizer.SpatialLocalizerId} that was not registered.");
            }
        }

        public void RegisterNetworkManager(INetworkManager networkManager)
        {
            if (!networkManagers.Add(networkManager))
            {
                Debug.LogError($"Attempted to register the same network manager multiple times");
                return;
            }

            RegisterEvents(networkManager);
        }

        public void UnregisterNetworkManager(INetworkManager networkManager)
        {
            if (!networkManagers.Remove(networkManager))
            {
                Debug.LogError($"Attempted to unregister a network manager that was not registered");
                return;
            }

            UnregisterEvents(networkManager);
        }

        /// <summary>
        /// Call to register an ITrackingObserver to use for determining tracking state.
        /// </summary>
        /// <param name="trackingObserver">Tracking observer used to determine tracking state.</param>
        public void RegisterTrackingObserver(ITrackingObserver trackingObserver)
        {
            if (this.trackingObserver != null)
            {
                Debug.LogError("Multiple tracking observers registered for the application.");
            }

            this.trackingObserver = trackingObserver;
        }

        /// <summary>
        /// Call to unregister an ITrackingObserver to use for determining tracking state.
        /// </summary>
        /// <param name="trackingObserver">Tracking observer used to determine tracking state.</param>
        public void UnregisterTrackingObserver(ITrackingObserver trackingObserver)
        {
            if (this.trackingObserver != trackingObserver)
            {
                Debug.LogWarning("Attempted to unregister tracking observer that wasn't registered.");
            }
            else
            {
                this.trackingObserver = null;
            }
        }

        public Task<bool> RunRemoteLocalizationAsync(INetworkConnection socketEndpoint, Guid spatialLocalizerID, ISpatialLocalizationSettings settings)
        {
            DebugLog($"Initiating remote localization: {socketEndpoint.Address}, {spatialLocalizerID.ToString()}");
            if (remoteLocalizationSessions.TryGetValue(socketEndpoint, out var currentCompletionSource))
            {
                DebugLog("Canceling existing remote localization session: {socketEndpoint.Address}");
                currentCompletionSource.TrySetCanceled();
                remoteLocalizationSessions.Remove(socketEndpoint);
            }

            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            remoteLocalizationSessions.Add(socketEndpoint, taskCompletionSource);

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(stream))
            {
                message.Write(LocalizeCommand);
                message.Write(spatialLocalizerID);
                settings.Serialize(message);

                socketEndpoint.Send(stream.ToArray());
            }

            return taskCompletionSource.Task;
        }

        public Task<bool> LocalizeAsync(INetworkConnection socketEndpoint, Guid spatialLocalizerID, ISpatialLocalizationSettings settings)
        {
            DebugLog("LocalizeAsync");
            if (!participants.TryGetValue(socketEndpoint, out SpatialCoordinateSystemParticipant participant))
            {
                Debug.LogError($"Could not find a SpatialCoordinateSystemParticipant for INetworkConnection {socketEndpoint.Address}");
                return Task.FromResult(false);
            }

            if (currentLocalizationSession != null)
            {
                if (participant == currentLocalizationSession.Peer &&
                    remoteLocalizationSessions.TryGetValue(socketEndpoint, out var taskCompletionSource) &&
                    taskCompletionSource.TrySetCanceled())
                {
                    DebugLog($"Current localization session for {socketEndpoint.Address} was canceled based on a new localization request.");
                    remoteLocalizationSessions.Remove(socketEndpoint);
                }
                else
                {
                    Debug.LogError($"Failed to start localization session because an existing localization session is in progress");
                    return Task.FromResult(false);
                }
            }

            if (!localizers.TryGetValue(spatialLocalizerID, out ISpatialLocalizer localizer))
            {
                Debug.LogError($"Could not find a ISpatialLocalizer for spatialLocalizerID {spatialLocalizerID}");
                return Task.FromResult(false);
            }

            DebugLog("Returning a localization session.");
            return RunLocalizationSessionAsync(localizer, settings, participant);
        }

        protected override void OnDestroy()
        {
            foreach (INetworkManager networkManager in networkManagers)
            {
                UnregisterEvents(networkManager);
            }

            CleanUpParticipants();
        }

        private void Update()
        {
            foreach (var participant in participants.Values)
            {
                participant.EnsureStateChangesAreBroadcast();
            }
        }

        private void SendLocalizationCompleteCommand(INetworkConnection socketEndpoint, bool localizationSuccessful)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(stream))
            {
                message.Write(LocalizationCompleteCommand);
                message.Write(localizationSuccessful);

                socketEndpoint.Send(stream.ToArray());
            }
        }

        private void OnConnected(INetworkConnection connection)
        {
            if (participants.ContainsKey(connection))
            {
                Debug.LogWarning("SpatialCoordinateSystemParticipant connected that already existed");
                return;
            }

            DebugLog($"Creating new SpatialCoordinateSystemParticipant, IPAddress: {connection.Address}, DebugLogging: {debugLogging}");

            SpatialCoordinateSystemParticipant participant = new SpatialCoordinateSystemParticipant(connection, debugVisual, debugVisualScale);
            participants[connection] = participant;
            participant.ShowDebugVisuals = showDebugVisuals;
            participant.SendSupportedLocalizersMessage(connection, localizers.Keys);

            if (ParticipantConnected == null)
            {
                DebugLog($"No ParticipantConnected event handlers exist");
            }
            else
            {
                DebugLog($"Invoking ParticipantConnected event");
                ParticipantConnected.Invoke(participant);
            }
        }

        private void OnDisconnected(INetworkConnection connection)
        {
            if (participants.TryGetValue(connection, out var participant))
            {
                participant.Dispose();
                participants.Remove(connection);

                ParticipantDisconnected?.Invoke(participant);
            }

            remoteLocalizationSessions.Remove(connection);
        }

        private void OnCoordinateStateReceived(INetworkConnection socketEndpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            if (!participants.TryGetValue(socketEndpoint, out SpatialCoordinateSystemParticipant participant))
            {
                Debug.LogError($"Failed to find a SpatialCoordinateSystemParticipant for an attached INetworkConnection");
                return;
            }

            participant.ReadCoordinateStateMessage(reader);
        }

        private void OnSupportedLocalizersMessageReceived(INetworkConnection socketEndpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            if (!participants.TryGetValue(socketEndpoint, out SpatialCoordinateSystemParticipant participant))
            {
                Debug.LogError($"Failed to find a SpatialCoordinateSystemParticipant for an attached INetworkConnection");
                return;
            }

            participant.ReadSupportedLocalizersMessage(reader);
        }

        private void OnLocalizationCompleteMessageReceived(INetworkConnection socketEndpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool localizationSuccessful = reader.ReadBoolean();

            if (!remoteLocalizationSessions.TryGetValue(socketEndpoint, out TaskCompletionSource<bool> taskSource))
            {
                DebugLog($"Remote session from connection {socketEndpoint.Address} completed but we were no longer tracking that session");
                return;
            }

            DebugLog($"Localization completed message received: {socketEndpoint.Address}");
            remoteLocalizationSessions.Remove(socketEndpoint);
            taskSource.SetResult(localizationSuccessful);
        }

        private async void OnLocalizeMessageReceived(INetworkConnection socketEndpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            DebugLog("LocalizeMessageReceived");
            if (!participants.TryGetValue(socketEndpoint, out SpatialCoordinateSystemParticipant participant))
            {
                Debug.LogError($"Could not find a SpatialCoordinateSystemParticipant for INetworkConnection {socketEndpoint.Address}");
                SendLocalizationCompleteCommand(socketEndpoint, localizationSuccessful: false);
                return;
            }

            if (currentLocalizationSession != null)
            {
                if (participant == currentLocalizationSession.Peer &&
                    remoteLocalizationSessions.TryGetValue(socketEndpoint, out var taskCompletionSource) &&
                    taskCompletionSource.TrySetCanceled())
                {
                    DebugLog($"Current localization session for {socketEndpoint.Address} was canceled based on a new localization request.");
                    remoteLocalizationSessions.Remove(socketEndpoint);
                }
                else
                {
                    Debug.LogError($"Failed to start localization session because an existing localization session is in progress that couldn't be canceled.");
                    SendLocalizationCompleteCommand(socketEndpoint, localizationSuccessful: false);
                    return;
                }
            }

            Guid spatialLocalizerID = reader.ReadGuid();

            if (!localizers.TryGetValue(spatialLocalizerID, out ISpatialLocalizer localizer))
            {
                Debug.LogError($"Request to begin localization with localizer {spatialLocalizerID} but no localizer with that ID was registered");
                SendLocalizationCompleteCommand(socketEndpoint, localizationSuccessful: false);
                return;
            }

            if (!localizer.TryDeserializeSettings(reader, out ISpatialLocalizationSettings settings))
            {
                Debug.LogError($"Failed to deserialize settings for localizer {spatialLocalizerID}");
                SendLocalizationCompleteCommand(socketEndpoint, localizationSuccessful: false);
                return;
            }

            bool localizationSuccessful = await RunLocalizationSessionAsync(localizer, settings, participant);

            // Ensure that the participant's fully-localized state is sent before sending the LocalizationComplete command (versus waiting
            // for the next Update). This way the remote peer receives the located state of the participant before they receive the notification
            // that this localization session completed.
            participant.EnsureStateChangesAreBroadcast();

            SendLocalizationCompleteCommand(socketEndpoint, localizationSuccessful);
        }

        private void OnParticipantDataReceived(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            if (!TryGetSpatialCoordinateSystemParticipant(connection, out SpatialCoordinateSystemParticipant participant))
            {
                Debug.LogError($"Received participant localization data for a missing participant: {connection.Address}");
                return;
            }

            if (participant.CurrentLocalizationSession == null)
            {
                Debug.LogError($"Received participant localization data for a participant that is not currently running a localization session: {connection.Address}");
                return;
            }

            DebugLog($"Data received for participant: {connection.Address}, {command}");
            participant.CurrentLocalizationSession.OnDataReceived(reader);
        }

        private async Task<bool> RunLocalizationSessionAsync(ISpatialLocalizer localizer, ISpatialLocalizationSettings settings, SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Creating localization session: {participant.NetworkConnection.Address}, {settings.ToString()}, {localizer.ToString()}");
            if (!localizer.TryCreateLocalizationSession(participant, settings, out ISpatialLocalizationSession currentLocalizationSession))
            {
                Debug.LogError($"Failed to create an ISpatialLocalizationSession from localizer {localizer.SpatialLocalizerId}");
                return false;
            }

            using (currentLocalizationSession)
            {
                DebugLog($"Setting localization session for participant: {participant.NetworkConnection.Address}, {currentLocalizationSession.ToString()}");
                participant.CurrentLocalizationSession = currentLocalizationSession;

                try
                {
                    DebugLog($"Starting localization: {participant.NetworkConnection.Address}, {currentLocalizationSession.ToString()}");
                    // Some SpatialLocalizers/SpatialCoordinateServices key off of token cancellation for their logic flow.
                    // Therefore, we need to create a cancellation token even it is never actually cancelled by the SpatialCoordinateSystemManager.
                    using (var localizeCTS = new CancellationTokenSource())
                    {
                        var coordinate = await currentLocalizationSession.LocalizeAsync(localizeCTS.Token);
                        participant.Coordinate = coordinate;
                    }
                }
                finally
                {
                    participant.CurrentLocalizationSession = null;
                }
            }
            currentLocalizationSession = null;
            return participant.Coordinate != null;
        }

        private void RegisterEvents(INetworkManager networkManager)
        {
            networkManager.Connected += OnConnected;
            networkManager.Disconnected += OnDisconnected;
            networkManager.RegisterCommandHandler(LocalizeCommand, OnLocalizeMessageReceived);
            networkManager.RegisterCommandHandler(LocalizationCompleteCommand, OnLocalizationCompleteMessageReceived);
            networkManager.RegisterCommandHandler(CoordinateStateMessageHeader, OnCoordinateStateReceived);
            networkManager.RegisterCommandHandler(SupportedLocalizersMessageHeader, OnSupportedLocalizersMessageReceived);
            networkManager.RegisterCommandHandler(SpatialCoordinateSystemParticipant.LocalizationDataExchangeCommand, OnParticipantDataReceived);
            
            if (networkManager.IsConnected)
            {
                var connections = networkManager.Connections;
                foreach (var connection in connections)
                {
                    OnConnected(connection);
                }
            }
        }

        private void UnregisterEvents(INetworkManager networkManager)
        {
            networkManager.Connected -= OnConnected;
            networkManager.Disconnected -= OnDisconnected;
            networkManager.UnregisterCommandHandler(LocalizeCommand, OnLocalizeMessageReceived);
            networkManager.UnregisterCommandHandler(LocalizationCompleteCommand, OnLocalizationCompleteMessageReceived);
            networkManager.UnregisterCommandHandler(CoordinateStateMessageHeader, OnCoordinateStateReceived);
            networkManager.UnregisterCommandHandler(SupportedLocalizersMessageHeader, OnSupportedLocalizersMessageReceived);
            networkManager.UnregisterCommandHandler(SpatialCoordinateSystemParticipant.LocalizationDataExchangeCommand, OnParticipantDataReceived);
        }

        private void CleanUpParticipants()
        {
            foreach(var participant in participants)
            {
                if (participant.Value != null)
                {
                    participant.Value.Dispose();
                }
            }

            participants.Clear();
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"SpatialCoordinateSystemManager: {message}");
            }
        }

        public bool TryGetSpatialCoordinateSystemParticipant(INetworkConnection connectedEndpoint, out SpatialCoordinateSystemParticipant participant)
        {
            return participants.TryGetValue(connectedEndpoint, out participant);
        }
    }
}
