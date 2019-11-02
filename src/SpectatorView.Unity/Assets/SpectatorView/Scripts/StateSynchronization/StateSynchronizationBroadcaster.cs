// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using CallerMemberNameAttribute = System.Runtime.CompilerServices.CallerMemberNameAttribute;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// This class observes changes and updates content on a user device.
    /// </summary>
    public class StateSynchronizationBroadcaster : NetworkManager<StateSynchronizationBroadcaster>
    {
        /// <summary>
        /// Check to enable debug logging.
        /// </summary>
        [Tooltip("Check to enable debug logging.")]
        [SerializeField]
        protected bool debugLogging;

        /// <summary>
        /// Port used for sending data.
        /// </summary>
        [Tooltip("Port used for sending data.")]
        public int Port = 7410;

        private const float PerfUpdateTimeSeconds = 1.0f;
        private float timeUntilNextPerfUpdate = PerfUpdateTimeSeconds;
        private int numFrames = 0;

        private GameObject dontDestroyOnLoadGameObject;

        private readonly List<AssetBundleSend> pendingAssetBundleSends = new List<AssetBundleSend>();

        protected override int RemotePort => Port;

        public event Action<SocketEndpoint> ConnectedAndReady;

        protected override void Awake()
        {
            DebugLog($"Awoken!");
            base.Awake();

            RegisterCommandHandler(StateSynchronizationObserver.SyncCommand, HandleSyncCommand);
            RegisterCommandHandler(StateSynchronizationObserver.PerfDiagnosticModeEnabledCommand, HandlePerfMonitoringModeEnableRequest);
            RegisterCommandHandler(StateSynchronizationObserver.AssetBundleRequestInfoCommand, HandleAssetBundleRequestInfo);
            RegisterCommandHandler(StateSynchronizationObserver.AssetBundleRequestDownloadCommand, HandleAssetBundleRequestDownload);
            RegisterCommandHandler(StateSynchronizationObserver.AssetLoadCompletedCommand, HandleAssetLoadCompleted);

            // Ensure that runInBackground is set to true so that the app continues to send network
            // messages even if it loses focus
            Application.runInBackground = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnregisterCommandHandler(StateSynchronizationObserver.SyncCommand, HandleSyncCommand);
            UnregisterCommandHandler(StateSynchronizationObserver.PerfDiagnosticModeEnabledCommand, HandlePerfMonitoringModeEnableRequest);
        }

        protected override void Start()
        {
            base.Start();

            SetupNetworkConnectionManager();
        }

        protected virtual void SetupNetworkConnectionManager()
        {
            if (connectionManager != null)
            {
                DebugLog("Setting up connection manager");

                connectionManager.StartListening(Port);
            }
            else
            {
                Debug.LogWarning("Connection Manager not defined for Broadcaster.");
            }
        }

        private void DebugLog(string message, [CallerMemberName] string callerMemberName = null)
        {
            if (debugLogging)
            {
                Debug.Log($"StateSynchronizationBroadcaster - {callerMemberName}: {message}", this);
            }
        }

        protected override void OnConnected(SocketEndpoint endpoint)
        {
            DebugLog($"Broadcaster received connection from {endpoint.Address}.");
            base.OnConnected(endpoint);
        }

        protected override void OnDisconnected(SocketEndpoint endpoint)
        {
            DebugLog($"Broadcaster received disconnect from {endpoint.Address}.");
            base.OnDisconnected(endpoint);
        }

        /// <summary>
        /// True if network connections exist, otherwise false
        /// </summary>
        public bool HasConnections
        {
            get
            {
                return connectionManager != null && connectionManager.HasConnections;
            }
        }

        /// <summary>
        /// Returns how many bytes have been queued to send to other devices
        /// </summary>
        public int OutputBytesQueued
        {
            get
            {
                return connectionManager.OutputBytesQueued;
            }
        }

        private void Update()
        {
            if (connectionManager == null)
            {
                return;
            }

            UpdateExtension();
            UpdatePendingAssetBundleSends();

            if (HasConnections && BroadcasterSettings.IsInitialized && BroadcasterSettings.Instance && BroadcasterSettings.Instance.AutomaticallyBroadcastAllGameObjects)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ComponentExtensions.EnsureComponent<TransformBroadcaster>(root);
                    }
                }

                // GameObjects that are marked DontDestroyOnLoad exist in a special scene, and that scene
                // cannot be enumerated via the SceneManager. The only way to access that scene is from a
                // GameObject inside that scene, so we need to create a GameObject we have access to inside
                // that scene in order to enumerate all of its root GameObjects.
                if (dontDestroyOnLoadGameObject == null)
                {
                    dontDestroyOnLoadGameObject = new GameObject("StateSynchronizationBroadcaster_DontDestroyOnLoad");
                    DontDestroyOnLoad(dontDestroyOnLoadGameObject);
                }

                foreach (GameObject root in dontDestroyOnLoadGameObject.scene.GetRootGameObjects())
                {
                    ComponentExtensions.EnsureComponent<TransformBroadcaster>(root);
                }
            }
        }

        /// <summary>
        /// Extension method called on update
        /// </summary>
        protected virtual void UpdateExtension() { }

        /// <summary>
        /// Called after a frame is completed to send state data to socket end points.
        /// </summary>
        public void OnFrameCompleted()
        {
            //Camera update
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                Transform camTrans = null;
                if (Camera.main != null &&
                    Camera.main.transform != null)
                {
                    camTrans = Camera.main.transform;
                }

                message.Write(StateSynchronizationObserver.CameraCommand);
                message.Write(Time.time);
                message.Write(camTrans != null ? camTrans.position : Vector3.zero);
                message.Write(camTrans != null ? camTrans.rotation : Quaternion.identity);
                message.Flush();

                connectionManager.Broadcast(memoryStream.ToArray());
            }

            //Perf
            timeUntilNextPerfUpdate -= Time.deltaTime;
            numFrames++;
            if (timeUntilNextPerfUpdate < 0)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter message = new BinaryWriter(memoryStream))
                {
                    message.Write(StateSynchronizationObserver.PerfCommand);
                    StateSynchronizationPerformanceMonitor.Instance.WriteMessage(message, numFrames);
                    message.Flush();
                    connectionManager.Broadcast(memoryStream.ToArray());
                }

                timeUntilNextPerfUpdate = PerfUpdateTimeSeconds;
                numFrames = 0;
            }
        }

        public void HandleSyncCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            reader.ReadSingle(); // float time
            StateSynchronizationSceneManager.Instance.ReceiveMessage(endpoint, reader);
        }

        private void HandlePerfMonitoringModeEnableRequest(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool enabled = reader.ReadBoolean();
            if (StateSynchronizationPerformanceMonitor.Instance != null)
            {
                StateSynchronizationPerformanceMonitor.Instance.SetDiagnosticMode(enabled);
            }
        }

        private bool TryGetAssetBundle(AssetBundlePlatform platform, out string versionIdentity, out string versionDisplayName, out byte[] data)
        {
            var assetPath = $"{platform}/{StateSynchronizationObserver.AssetBundleName}";

            var assetBundle = Resources.Load<TextAsset>(assetPath);
            var version = Resources.Load<AssetBundleVersion>($"{assetPath}.version");

            if ((assetBundle == null) || (version == null))
            {
                versionIdentity = default;
                versionDisplayName = default;
                data = default;
            }
            else
            {
                versionIdentity = version.Identity;
                versionDisplayName = version.DisplayName;
                data = assetBundle.bytes;
            }

            if (assetBundle != null)
            {
                Resources.UnloadAsset(assetBundle);
            }

            if (version != null)
            {
                Resources.UnloadAsset(version);
            }

            return (data != null);
        }

        private void HandleAssetBundleRequestInfo(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            AssetBundlePlatform platform = (AssetBundlePlatform)reader.ReadByte();
            DebugLog($"Received asset bundle info request for platform {platform}");

            bool hasAsset = TryGetAssetBundle(platform, out var versionIdentity, out var versionDisplayName, out _);

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateSynchronizationObserver.AssetBundleReportInfoCommand);
                writer.Write(hasAsset);

                if (hasAsset)
                {
                    writer.Write(versionIdentity);
                    writer.Write(versionDisplayName);
                }

                endpoint.Send(stream.ToArray());
            }
        }

        private void HandleAssetBundleRequestDownload(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            AssetBundlePlatform platform = (AssetBundlePlatform)reader.ReadByte();

            bool hasAsset = TryGetAssetBundle(platform, out var versionIdentity, out var versionDisplayName, out var data);

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateSynchronizationObserver.AssetBundleReportDownloadStartCommand);
                writer.Write(hasAsset);

                if (hasAsset)
                {
                    DebugLog($"Starting {StateSynchronizationObserver.FormatBytes(data.Length)} asset bundle send to {endpoint.Address}. Bundle: {AssetBundleVersion.Format(versionIdentity, versionDisplayName)}...");

                    writer.Write(versionIdentity);
                    writer.Write(versionDisplayName);
                    writer.Write(data.Length);

                    pendingAssetBundleSends.Add(new AssetBundleSend
                    {
                        Recipient = endpoint,
                        Data = data,
                        NextDataToSendIndex = 0,
                    });
                }
                else
                {
                    DebugLog($"Unexpectedly received asset bundle download request for platform {platform} from {endpoint.Address}.");
                }

                endpoint.Send(stream.ToArray());
            }
        }

        private void HandleAssetLoadCompleted(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            DebugLog($"Asset loading is complete for {endpoint.Address}, sending the {nameof(ConnectedAndReady)} event.");

            // Notify everyone the connection is actually ready
            ConnectedAndReady?.Invoke(endpoint);
        }

        private void UpdatePendingAssetBundleSends()
        {
            for (int iPendingSend = (pendingAssetBundleSends.Count - 1); iPendingSend >= 0; iPendingSend--)
            {
                var pendingSend = pendingAssetBundleSends[iPendingSend];

                if (pendingSend.Recipient.IsConnected)
                {
                    using (MemoryStream stream = new MemoryStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(StateSynchronizationObserver.AssetBundleReportDownloadDataCommand);

                        int bytesRemaining = (pendingSend.Data.Length - pendingSend.NextDataToSendIndex);
                        Debug.Assert(bytesRemaining > 0, this);

                        int bytesToSend = Math.Min(StateSynchronizationObserver.AssetBundleReportDownloadDataMaxByteCount, bytesRemaining);

                        writer.Write(pendingSend.Data, pendingSend.NextDataToSendIndex, bytesToSend);
                        pendingSend.NextDataToSendIndex += bytesToSend;

                        pendingSend.Recipient.Send(stream.ToArray());
                    }

                    if (pendingSend.NextDataToSendIndex == pendingSend.Data.Length)
                    {
                        DebugLog($"Completed {StateSynchronizationObserver.FormatBytes(pendingSend.Data.Length)} asset bundle send to {pendingSend.Recipient.Address}.");
                        pendingAssetBundleSends.RemoveAt(iPendingSend);
                    }
                    else
                    {
                        DebugLog($"Sent {StateSynchronizationObserver.FormatByteProgress(pendingSend.NextDataToSendIndex, pendingSend.Data.Length)} of asset bundle to {pendingSend.Recipient.Address}. Waiting to send more...");
                    }
                }
                else
                {
                    DebugLog($"Abandoning asset bundle send, because observer {pendingSend.Recipient.Address} disconnected.");
                    pendingAssetBundleSends.RemoveAt(iPendingSend);
                }
            }
        }

        private class AssetBundleSend
        {
            public SocketEndpoint Recipient;
            public byte[] Data;
            public int NextDataToSendIndex;
        }
    }
}
