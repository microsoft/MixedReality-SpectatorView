// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CallerMemberNameAttribute = System.Runtime.CompilerServices.CallerMemberNameAttribute;

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
        public const string AssetBundleRequestInfoCommand = "RequestAssetBundleInfo";
        public const string AssetBundleReportInfoCommand = "ReportAssetBundleInfo";
        public const string AssetBundleRequestDownloadCommand = "RequestAssetBundleDownload";
        public const string AssetBundleReportDownloadStartCommand = "ReportAssetBundleDownloadStart";
        public const string AssetBundleReportDownloadDataCommand = "ReportAssetBundleDownloadData";
        public const string AssetLoadCompletedCommand = "AssetLoadCompleted";

        public const string AssetBundleName = "spectatorview";
        public const int AssetBundleReportDownloadDataMaxByteCount = 256 * 1024;

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

        private double[] averageTimePerFeature;
        private const float heartbeatTimeInterval = 0.1f;
        private float timeSinceLastHeartbeat = 0.0f;
        private HologramSynchronizer hologramSynchronizer = new HologramSynchronizer();

        private AssetBundle currentAssetBundle;
        private string currentAssetBundleIdentity;
        private string currentAssetBundleDisplayName;
        private AssetBundleReceive pendingAssetBundleReceive;

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
            RegisterCommandHandler(AssetBundleReportDownloadStartCommand, HandleAssetBundleDownloadStartCommand);
            RegisterCommandHandler(AssetBundleReportDownloadDataCommand, HandleAssetBundleDownloadDataCommand);

            AssetCache.AssetCacheCountChanged += AssetCacheCountChanged;
        }

        protected override void OnDestroy()
        {
            AssetCache.AssetCacheCountChanged -= AssetCacheCountChanged;

            base.OnDestroy();
        }

        protected void Update()
        {
            CheckAndSendHeartbeat();
            hologramSynchronizer.UpdateHolograms();
        }

        private void DebugLog(string message, [CallerMemberName] string callerMemberName = null)
        {
            if (debugLogging)
            {
                string connectedState = IsConnected ? $"Connected - {ConnectedIPAddress}" : "Not Connected";
                Debug.Log($"StateSynchronizationObserver - {callerMemberName} - {connectedState}: {message}", this);
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
            // TODO: should this be called here?!?!?!  ResetAssetCaches();

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
            int featureCount = reader.ReadInt32();

            if (averageTimePerFeature == null)
            {
                averageTimePerFeature = new double[featureCount];
            }

            for (int i = 0; i < featureCount; i++)
            {
                averageTimePerFeature[i] = reader.ReadSingle();
            }
        }

        private void HandleAssetBundleInfoCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool hasAssetBundle = reader.ReadBoolean();
            if (hasAssetBundle)
            {
                var assetBundleIdentity = reader.ReadString();
                var assetBundleDisplayName = reader.ReadString();

                if (assetBundleIdentity == currentAssetBundleIdentity)
                {
                    DebugLog($"Not requesting asset bundle download.  Already have asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)}.");
                    SendAssetsLoaded(endpoint);
                }
                else
                {
                    DebugLog($"Requesting asset bundle download for {AssetBundleVersion.Format(assetBundleIdentity, assetBundleDisplayName)}...");
                    SendAssetBundleDownloadRequest(endpoint);
                }
            }
            else
            {
                DebugLog($"Not requesting asset bundle download.  None is available for platform {AssetBundlePlatformInfo.Current}.");
                SendAssetsLoaded(endpoint);
            }
        }

        private void HandleAssetBundleDownloadStartCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            bool hasAssetBundle = reader.ReadBoolean();

            if (hasAssetBundle)
            {
                ResetAssetCaches();

                currentAssetBundleIdentity = reader.ReadString();
                currentAssetBundleDisplayName = reader.ReadString();

                pendingAssetBundleReceive = new AssetBundleReceive
                {
                    Data = new byte[reader.ReadInt32()],
                    NextDataToReceiveIndex = 0,
                };

                DebugLog($"Receiving asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)} with {pendingAssetBundleReceive.Data.Length:N0} bytes...");
            }
            else
            {
                DebugLog($"Unexpectedly got no asset bundle for platform {AssetBundlePlatformInfo.Current}.");
                SendAssetsLoaded(endpoint);
            }
        }

        private void HandleAssetBundleDownloadDataCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            if (pendingAssetBundleReceive == null)
            {
                DebugLog($"Unexpected command.  There is no {nameof(pendingAssetBundleReceive)}.");
            }
            else
            {
                Debug.Assert(currentAssetBundle == null, this);

                var newData = reader.ReadBytes(remainingDataSize);

                if ((pendingAssetBundleReceive.NextDataToReceiveIndex + newData.Length) > pendingAssetBundleReceive.Data.Length)
                {
                    DebugLog($"Unexpectedly got too much data for {nameof(pendingAssetBundleReceive)}.");
                    ResetAssetCaches();
                }
                else
                {
                    System.Array.Copy(newData, 0, pendingAssetBundleReceive.Data, pendingAssetBundleReceive.NextDataToReceiveIndex, newData.Length);
                    pendingAssetBundleReceive.NextDataToReceiveIndex += newData.Length;

                    if (pendingAssetBundleReceive.NextDataToReceiveIndex == pendingAssetBundleReceive.Data.Length)
                    {
                        DebugLog($"Successfully received all {pendingAssetBundleReceive.Data.Length:N0} bytes of asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)}.  Loading its assets...");
                        currentAssetBundle = AssetBundle.LoadFromMemory(pendingAssetBundleReceive.Data);
                        pendingAssetBundleReceive = null;

                        DebugLog($"Successfully loaded asset bundle.  Loading all assets from bundle...");
                        currentAssetBundle.LoadAllAssets();

                        DebugLog($"All assets loaded from bundle.");

                        SendAssetsLoaded(endpoint);
                    }
                    else
                    {
                        var percentComplete = (100.0 * pendingAssetBundleReceive.NextDataToReceiveIndex / pendingAssetBundleReceive.Data.Length);

                        DebugLog($"Received {pendingAssetBundleReceive.NextDataToReceiveIndex:N0}/{pendingAssetBundleReceive.Data.Length:N0} bytes of asset bundle ({percentComplete:N2}%).  Waiting for more...");
                    }
                }
            }
        }

        public string AssetStatus { get; private set; }

        public bool AssetStatusIsError { get; private set; }

        public event System.Action<string, bool> AssetStatusChanged;

        internal int PerformanceFeatureCount
        {
            get { return averageTimePerFeature?.Length ?? 0; }
        }

        internal IReadOnlyList<double> AverageTimePerFeature
        {
            get { return averageTimePerFeature; }
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

            currentAssetBundleIdentity = null;
            currentAssetBundleDisplayName = null;
            pendingAssetBundleReceive = null;
        }

        private void AssetCacheCountChanged(int assetCacheCount)
        {
            // TODO:?
        }

        private class AssetBundleReceive
        {
            public byte[] Data;
            public int NextDataToReceiveIndex;
        }
    }
}
