// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// This class observes changes and updates content on a spectator device.
    /// </summary>
    public class StateSynchronizationObserver : NetworkManager<StateSynchronizationObserver>
    {
        public const string SyncCommand = "SYNC";
        public const string CameraCommand = "Camera";
        public const string PerfCommand = "Perf";
        public const string PerfDiagnosticModeEnabledCommand = "PERFDIAG";
        public const string AssetBundleRequestInfoCommand = "RequestAssetBundleInfo";
        public const string AssetBundleReportInfoCommand = "ReportAssetBundleInfo";
        public const string AssetBundleRequestDownloadCommand = "RequestAssetBundleDownload";
        public const string AssetBundleReportDownloadCommand = "ReportAssetBundleDownload";
        public const string AssetLoadCompletedCommand = "AssetLoadCompleted";

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
        [SerializeField]
        protected int port = 7410;

        private const float heartbeatTimeInterval = 0.1f;
        private float timeSinceLastHeartbeat = 0.0f;
        private HologramSynchronizer hologramSynchronizer = new HologramSynchronizer();
        private StateSynchronizationPerformanceMonitor.ParsedMessage lastPerfMessage = new StateSynchronizationPerformanceMonitor.ParsedMessage(false, null, null);
        private AssetBundle currentAssetBundle;

        private static readonly byte[] heartbeatMessage = GenerateHeartbeatMessage();

        protected override int RemotePort => port;

        protected override void Awake()
        {
            DebugLog($"Awoken!");
            base.Awake();

            // Ensure that runInBackground is set to true so that the app continues to send network
            // messages even if it loses focus
            Application.runInBackground = true;

            if (connectionManager != null)
            {
                DebugLog("Setting up connection manager");

                // Start listening to incoming connections as well.
                connectionManager.StartListening(port);
            }
            else
            {
                Debug.LogError("Connection manager not specified for Observer.");
            }

            RegisterCommandHandler(SyncCommand, HandleSyncCommand);
            RegisterCommandHandler(CameraCommand, HandleCameraCommand);
            RegisterCommandHandler(PerfCommand, HandlePerfCommand);
            RegisterCommandHandler(AssetBundleReportInfoCommand, HandleAssetBundleInfoCommand);
            RegisterCommandHandler(AssetBundleReportDownloadCommand, HandleAssetBundleDownloadCommand);
        }

        protected void Update()
        {
            CheckAndSendHeartbeat();
            hologramSynchronizer.UpdateHolograms();
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                string connectedState = IsConnected ? $"Connected - {ConnectedIPAddress}" : "Not Connected";
                Debug.Log($"StateSynchronizationObserver - {connectedState}: {message}");
            }
        }

        protected override void OnConnected(SocketEndpoint endpoint)
        {
            base.OnConnected(endpoint);

            DebugLog($"Observer Connected to endpoint: {endpoint.Address}");

            if (StateSynchronizationSceneManager.IsInitialized)
            {
                StateSynchronizationSceneManager.Instance.MarkSceneDirty();
            }

            hologramSynchronizer.Reset(endpoint);
            ResetAssetCaches();

            SendAssetBundleInfoRequest(endpoint);
        }

        public void HandleCameraCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterCameraUpdate(timeStamp);
            transform.position = reader.ReadVector3();
            transform.rotation = reader.ReadQuaternion();
        }

        public void HandleSyncCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterFrameData(reader.ReadBytes(remainingDataSize), timeStamp);
        }

        public void HandlePerfCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            StateSynchronizationPerformanceMonitor.ReadMessage(reader, out lastPerfMessage);
        }

        public void SetPerformanceMonitoringMode(bool enabled)
        {
            if (connectionManager != null &&
                connectionManager.HasConnections)
            {
                byte[] message;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(PerfDiagnosticModeEnabledCommand);
                    writer.Write(enabled);
                    writer.Flush();
                    message = stream.ToArray();
                }

                connectionManager.Broadcast(message);
            }
        }

        internal bool PerformanceMonitoringModeEnabled => lastPerfMessage.PerformanceMonitoringEnabled;
        internal IReadOnlyList<Tuple<string, double>> PerformanceEventDurations => lastPerfMessage.EventDurations;
        internal IReadOnlyList<Tuple<string, int>> PerformanceEventCounts => lastPerfMessage.EventCounts;

        private void HandleAssetBundleInfoCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool hasAssetBundle = reader.ReadBoolean();
            if (hasAssetBundle)
            {
                DebugLog($"We determined that there is an asset bundle for this platform");
                SendAssetBundleDownloadRequest(endpoint);
            }
            else
            {
                DebugLog($"We determined that there is NOT an asset bundle for this platform");
                SendAssetsLoaded(endpoint);
            }
        }

        private void HandleAssetBundleDownloadCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            int length = reader.ReadInt32();
            byte[] rawAssetBundle;
            if (length > 0)
            {
                rawAssetBundle = reader.ReadBytes(length);
                Debug.Log("Loading asset bundle");
                currentAssetBundle = AssetBundle.LoadFromMemory(rawAssetBundle);
                Debug.Log("Asset bundle load completed");
                currentAssetBundle.LoadAllAssets();
            }

            SendAssetsLoaded(endpoint);
        }

        private void CheckAndSendHeartbeat()
        {
            if (connectionManager != null &&
                connectionManager.HasConnections)
            {
                timeSinceLastHeartbeat += Time.deltaTime;
                if (timeSinceLastHeartbeat > heartbeatTimeInterval)
                {
                    timeSinceLastHeartbeat = 0.0f;
                    connectionManager.Broadcast(heartbeatMessage);
                }
            }
        }

        private static byte[] GenerateHeartbeatMessage()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // It doesn't matter what the content of this message is, it just can't conflict with other commands
                // sent in this channel and read by the Broadcaster.
                writer.Write("♥");
                writer.Flush();

                return stream.ToArray();
            }
        }

        private void SendAssetBundleInfoRequest(SocketEndpoint endpoint)
        {
            DebugLog($"Sending a request for asset bundle info for {AssetBundlePlatformInfo.Current}");
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(AssetBundleRequestInfoCommand);
                writer.Write((byte)AssetBundlePlatformInfo.Current);

                endpoint.Send(stream.ToArray());
            }
        }

        private void SendAssetBundleDownloadRequest(SocketEndpoint endpoint)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(AssetBundleRequestDownloadCommand);
                writer.Write((byte)AssetBundlePlatformInfo.Current);

                endpoint.Send(stream.ToArray());
            }
        }

        private void SendAssetsLoaded(SocketEndpoint endpoint)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(AssetLoadCompletedCommand);

                endpoint.Send(stream.ToArray());
            }
        }

        private void ResetAssetCaches()
        {
            foreach (AssetCache cache in FindObjectsOfType<AssetCache>())
            {
                Destroy(cache);
            }

            if (currentAssetBundle != null)
            {
                currentAssetBundle.Unload(unloadAllLoadedObjects: true);
                currentAssetBundle = null;
            }
        }
    }
}
