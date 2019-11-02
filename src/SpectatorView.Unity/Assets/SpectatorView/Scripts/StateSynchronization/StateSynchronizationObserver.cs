// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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
        public const string PerfDiagnosticModeEnabledCommand = "PERFDIAG";
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

        private const float heartbeatTimeInterval = 0.1f;
        private float timeSinceLastHeartbeat = 0.0f;
        private HologramSynchronizer hologramSynchronizer = new HologramSynchronizer();
        private StateSynchronizationPerformanceMonitor.ParsedMessage lastPerfMessage = new StateSynchronizationPerformanceMonitor.ParsedMessage(false, null, null);

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
            AssetState = new AssetState
            {
                Status = (AssetCache.AssetCacheCount > 0) ? AssetStateStatus.Preloaded : AssetStateStatus.None
            };
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

            AssetState = new AssetState
            {
                Status = AssetStateStatus.RequestingAssetBundle,
                AssetBundleDisplayName = currentAssetBundleDisplayName,
            };

            SendAssetBundleInfoRequest(endpoint);
        }

        private void HandleCameraCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterCameraUpdate(timeStamp);
            transform.position = reader.ReadVector3();
            transform.rotation = reader.ReadQuaternion();
        }

        private void HandleSyncCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterFrameData(reader.ReadBytes(remainingDataSize), timeStamp);
        }

        private void HandlePerfCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
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
                var assetBundleIdentity = reader.ReadString();
                var assetBundleDisplayName = reader.ReadString();

                if (assetBundleIdentity == currentAssetBundleIdentity)
                {
                    DebugLog($"Not requesting asset bundle download. Already have asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)}.");

                    AssetState = new AssetState
                    {
                        Status = AssetStateStatus.AssetBundleLoaded,
                        AssetBundleDisplayName = currentAssetBundleDisplayName,
                    };

                    SendAssetsLoaded(endpoint);
                }
                else
                {
                    DebugLog($"Requesting asset bundle download for {AssetBundleVersion.Format(assetBundleIdentity, assetBundleDisplayName)}...");
                    Debug.Assert(AssetState.Status == AssetStateStatus.RequestingAssetBundle, this);
                    SendAssetBundleDownloadRequest(endpoint);
                }
            }
            else
            {
                DebugLog($"Not requesting asset bundle download. None is available for platform {AssetBundlePlatformInfo.Current}.");

                Debug.Assert(currentAssetBundle == null, "If we already have an asset bundle loaded, but the remote user doesn't have one, it probably means we have mismatched assets... how did we get into this state?  Should we clear assets?", this);

                AssetState = new AssetState
                {
                    Status = (AssetCache.AssetCacheCount > 0) ? AssetStateStatus.Preloaded : AssetStateStatus.NonePreloadedAndNoAssetBundleAvailable,
                };

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

                DebugLog($"Receiving asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)} with {FormatBytes(pendingAssetBundleReceive.Data.Length)}...");

                AssetState = new AssetState
                {
                    Status = AssetStateStatus.DownloadingAssetBundle,
                    AssetBundleDisplayName = currentAssetBundleDisplayName,
                    BytesSoFar = pendingAssetBundleReceive.NextDataToReceiveIndex,
                    TotalBytes = pendingAssetBundleReceive.Data.Length,
                };
            }
            else
            {
                DebugLog($"Unexpectedly got no asset bundle for platform {AssetBundlePlatformInfo.Current}.");

                AssetState = new AssetState
                {
                    Status = (AssetCache.AssetCacheCount > 0) ? AssetStateStatus.Preloaded : AssetStateStatus.NonePreloadedAndNoAssetBundleAvailable,
                };

                SendAssetsLoaded(endpoint);
            }
        }

        private void HandleAssetBundleDownloadDataCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            if (pendingAssetBundleReceive == null)
            {
                DebugLog($"Unexpected command. There is no {nameof(pendingAssetBundleReceive)}.");
            }
            else
            {
                Debug.Assert(currentAssetBundle == null, this);

                var newData = reader.ReadBytes(remainingDataSize);

                if ((pendingAssetBundleReceive.NextDataToReceiveIndex + newData.Length) > pendingAssetBundleReceive.Data.Length)
                {
                    DebugLog($"Unexpectedly got too much data for {nameof(pendingAssetBundleReceive)}.");

                    AssetState = new AssetState
                    {
                        Status = AssetStateStatus.ErrorDownloadingAssetBundle,
                        AssetBundleDisplayName = currentAssetBundleDisplayName,
                        ErrorDetails = $"Unexpectedly got too much data.",
                    };

                    ResetAssetCaches();
                }
                else
                {
                    System.Array.Copy(newData, 0, pendingAssetBundleReceive.Data, pendingAssetBundleReceive.NextDataToReceiveIndex, newData.Length);
                    pendingAssetBundleReceive.NextDataToReceiveIndex += newData.Length;

                    if (pendingAssetBundleReceive.NextDataToReceiveIndex == pendingAssetBundleReceive.Data.Length)
                    {
                        DebugLog($"Successfully received all {FormatBytes(pendingAssetBundleReceive.Data.Length)} of asset bundle {AssetBundleVersion.Format(currentAssetBundleIdentity, currentAssetBundleDisplayName)}. Loading its assets...");

                        try
                        {
                            currentAssetBundle = AssetBundle.LoadFromMemory(pendingAssetBundleReceive.Data);
                            pendingAssetBundleReceive = null;

                            DebugLog($"Successfully loaded asset bundle. Loading all assets from bundle...");
                            currentAssetBundle.LoadAllAssets();
                        }
                        catch (System.Exception ex)
                        {
                            AssetState = new AssetState
                            {
                                Status = AssetStateStatus.ErrorLoadingAssetBundle,
                                AssetBundleDisplayName = currentAssetBundleDisplayName,
                                ErrorDetails = $"{ex.GetType()} - {ex.Message}",
                            };

                            ResetAssetCaches();
                            return;
                        }

                        DebugLog($"All assets loaded from bundle.");

                        AssetState = new AssetState
                        {
                            Status = AssetStateStatus.AssetBundleLoaded,
                            AssetBundleDisplayName = currentAssetBundleDisplayName,
                        };

                        SendAssetsLoaded(endpoint);
                    }
                    else
                    {
                        DebugLog($"Received {FormatByteProgress(pendingAssetBundleReceive.NextDataToReceiveIndex, pendingAssetBundleReceive.Data.Length)} of asset bundle. Waiting for more...");

                        AssetState = new AssetState
                        {
                            Status = AssetStateStatus.DownloadingAssetBundle,
                            AssetBundleDisplayName = currentAssetBundleDisplayName,
                            BytesSoFar = pendingAssetBundleReceive.NextDataToReceiveIndex,
                            TotalBytes = pendingAssetBundleReceive.Data.Length,
                        };
                    }
                }
            }
        }

        private AssetState assetState = new AssetState { Status = AssetStateStatus.Unknown };
        public AssetState AssetState
        {
            get { return assetState; }

            private set
            {
                assetState = value;
                AssetStateChanged?.Invoke(assetState);
            }
        }

        public event System.Action<AssetState> AssetStateChanged;

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
            switch (AssetState.Status)
            {
                case AssetStateStatus.None:
                    if (assetCacheCount > 0)
                    {
                        AssetState = new AssetState { Status = AssetStateStatus.Preloaded };
                    }
                    break;

                case AssetStateStatus.Preloaded:
                    if (assetCacheCount <= 0)
                    {
                        AssetState = new AssetState { Status = AssetStateStatus.None };
                    }
                    break;

                case AssetStateStatus.RequestingAssetBundle:
                case AssetStateStatus.DownloadingAssetBundle:
                case AssetStateStatus.AssetBundleLoaded:
                case AssetStateStatus.NonePreloadedAndNoAssetBundleAvailable:
                case AssetStateStatus.ErrorDownloadingAssetBundle:
                case AssetStateStatus.ErrorLoadingAssetBundle:
                    // No adjustments needed
                    break;

                case AssetStateStatus.Unknown:
                default:
                    Debug.LogError($"Unexpected asset state status \"{AssetState.Status}\".", this);
                    break;
            }
        }

        public static string FormatByteProgress(int bytesSoFar, int totalBytes)
        {
            GetBytesFormat(totalBytes, out float bytesDivisor, out string bytesFormat, out string bytesUnitSpecifier);

            var bytesSoFarText = (bytesSoFar / bytesDivisor).ToString(bytesFormat);
            var totalBytesText = (totalBytes / bytesDivisor).ToString(bytesFormat);

            var percentCompleteText = (100.0 * bytesSoFar / totalBytes).ToString("N1");

            return $"{percentCompleteText}% ({bytesSoFarText}/{totalBytesText} {bytesUnitSpecifier})";
        }

        public static string FormatBytes(int totalBytes)
        {
            GetBytesFormat(totalBytes, out float divisor, out string format, out string unitSpecifier);

            return $"{(totalBytes / divisor).ToString(format)} {unitSpecifier}";
        }

        private static void GetBytesFormat(int totalBytes, out float divisor, out string format, out string unitSpecifier)
        {
            if (totalBytes >= 1024 * 1024)
            {
                divisor = 1024 * 1024;
                format = "N1";
                unitSpecifier = "MB";
            }
            else if (totalBytes >= 1024)
            {
                divisor = 1024;
                format = "N1";
                unitSpecifier = "KB";
            }
            else
            {
                divisor = 1;
                format = "N0";
                unitSpecifier = "B";
            }
        }

        private class AssetBundleReceive
        {
            public byte[] Data;
            public int NextDataToReceiveIndex;
        }
    }

    public enum AssetStateStatus
    {
        Unknown,

        None,
        Preloaded,

        RequestingAssetBundle,
        DownloadingAssetBundle,
        AssetBundleLoaded,

        NonePreloadedAndNoAssetBundleAvailable,
        ErrorDownloadingAssetBundle,
        ErrorLoadingAssetBundle,
    }

    public struct AssetState
    {
        public AssetStateStatus Status;

        /// <summary>
        /// For <see cref="Status"/> associated with an asset bundle, this is the asset bundle's display name. Otherwise, undefined.
        /// </summary>
        public string AssetBundleDisplayName;

        /// <summary>
        /// For <see cref="Status"/> that has partial bytes associated with it, the byte count so far. Otherwise, undefined.
        /// </summary>
        public int BytesSoFar;

        /// <summary>
        /// For <see cref="Status"/> that has total bytes associated with it, the total byte count. Otherwise, undefined.
        /// </summary>
        public int TotalBytes;

        /// <summary>
        /// For <see cref="Status"/> that indicates an error, the error message. Otherwise, undefined.
        /// </summary>
        public string ErrorDetails;
    }
}
