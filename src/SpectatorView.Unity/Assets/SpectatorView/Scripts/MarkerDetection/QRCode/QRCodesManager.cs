// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Assuming assembly reference 'System.Numerics.Vectors, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' used by 'Windows.Foundation.UniversalApiContract' matches
// identity 'System.Numerics.Vectors, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' of 'System.Numerics.Vectors', you may need to supply runtime policy
#pragma warning disable 1701

#if UNITY_EDITOR || UNITY_WSA
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

#if WINDOWS_UWP
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
using Microsoft.MixedReality.PhotoCapture;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    public class QRCodesManager : MonoBehaviour
    {
        [Tooltip("Determines if the QR codes scanner should be automatically started.")]
        public bool AutoStartQRWatching = false;
        public bool IsWatcherRunning { get; private set; }
        public bool DebugLogging { get; set; }

        public event EventHandler<QRCode> QRCodeAdded;
        public event EventHandler<QRCode> QRCodeUpdated;
        public event EventHandler<QRCode> QRCodeRemoved;

        private SortedDictionary<System.Guid, QRCode> qrCodesList = new SortedDictionary<System.Guid, QRCode>();
        private QRCodeWatcher qrWatcher;
        private object lockObj = new object();
        private Task<QRCodeWatcherAccessStatus> startWatcherTask = null;
        private CancellationTokenSource startWatcherCTS = null;
        private Task stopTrackerTask = null;

        private static QRCodesManager qrCodesManager;
        public static QRCodesManager FindOrCreateQRCodesManager(GameObject gameObject)
        {
            if (qrCodesManager != null)
                return qrCodesManager;

            qrCodesManager = FindObjectOfType<QRCodesManager>();
            if (qrCodesManager != null)
                return qrCodesManager;

            Debug.Log("QRCodesManager created in scene");
            qrCodesManager = gameObject.AddComponent<QRCodesManager>();
            return qrCodesManager;
        }

        /// <summary>
        /// Tries to obtain the QRCode location in Unity Space.
        /// The position component of the location matrix will be at the top left of the QRCode
        /// The orientation of the location matrix will reflect the following axii:
        /// x axis: horizontal with the QRCode.
        /// y axis: positive direction down the QRCode.
        /// z axis: positive direction outward from the QRCode.
        /// Note: This function should be called from the main thread
        /// </summary>
        /// <param name="spatialGraphNodeId">QRCode SpatialGraphNodeId</param>
        /// <param name="location">Output location for the QRCode in Unity Space</param>
        /// <returns>returns true if the QRCode was located</returns>
        public bool TryGetLocationForQRCode(Guid spatialGraphNodeId, out Matrix4x4 location)
        {
            location = Matrix4x4.identity;

#if WINDOWS_UWP
            try
            {
                var coordinateSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(spatialGraphNodeId);
                return TryGetLocationForQRCode(coordinateSystem, out location);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown creating coordinate system for qr code id: {spatialGraphNodeId.ToString()}, {e.ToString()}");
                return false;
            }

#else
            Debug.LogError($"Failed to create coordinate system for qr code id: {spatialGraphNodeId.ToString()}");
            return false;
#endif
        }

#if WINDOWS_UWP
        /// <summary>
        /// Tries to obtain the QRCode location in Unity Space.
        /// The position component of the location matrix will be at the top left of the QRCode
        /// The orientation of the location matrix will reflect the following axii:
        /// x axis: horizontal with the QRCode.
        /// y axis: positive direction down the QRCode.
        /// z axis: positive direction outward from the QRCode.
        /// /// Note: This function should be called from the main thread
        /// </summary>
        /// <param name="coordinateSystem">QRCode SpatialCoordinateSystem</param>
        /// <param name="location">Output location for the QRCode in Unity Space</param>
        /// <returns>returns true if the QRCode was located</returns>
        public bool TryGetLocationForQRCode(SpatialCoordinateSystem coordinateSystem, out Matrix4x4 location)
        {
            location = Matrix4x4.identity;
            if (coordinateSystem != null)
            {
                try
                {
                    var appSpatialCoordinateSystem = WinRTExtensions.GetSpatialCoordinateSystem(UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr());
                    if (appSpatialCoordinateSystem != null)
                    {
                        // Get the relative transform from the unity origin
                        System.Numerics.Matrix4x4? relativePose = coordinateSystem.TryGetTransformTo(appSpatialCoordinateSystem);
                        if (relativePose != null)
                        {
                            System.Numerics.Matrix4x4 newMatrix = relativePose.Value;

                            // Platform coordinates are all right handed and unity uses left handed matrices. so we convert the matrix
                            // from rhs-rhs to lhs-lhs
                            // Convert from right to left coordinate system
                            newMatrix.M13 = -newMatrix.M13;
                            newMatrix.M23 = -newMatrix.M23;
                            newMatrix.M43 = -newMatrix.M43;

                            newMatrix.M31 = -newMatrix.M31;
                            newMatrix.M32 = -newMatrix.M32;
                            newMatrix.M34 = -newMatrix.M34;

                            System.Numerics.Vector3 winrtScale;
                            System.Numerics.Quaternion winrtRotation;
                            System.Numerics.Vector3 winrtTranslation;
                            System.Numerics.Matrix4x4.Decompose(newMatrix, out winrtScale, out winrtRotation, out winrtTranslation);

                            var translation = new Vector3(winrtTranslation.X, winrtTranslation.Y, winrtTranslation.Z);
                            var rotation = new Quaternion(winrtRotation.X, winrtRotation.Y, winrtRotation.Z, winrtRotation.W);
                            location = Matrix4x4.TRS(translation, rotation, Vector3.one);

                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("QRCode location unknown or not yet available.");
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Failed to obtain coordinate system for application");
                        return false;
                    }
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Note: TryGetLocationForQRCode needs to be called from main thread: {e}");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Failed to obtain coordinate system for QRCode");
                return false;
            }
        }
#endif // WINDOWS_UWP

        public Guid GetIdForQRCode(string qrCodeData)
        {
            lock (qrCodesList)
            {
                foreach (var ite in qrCodesList)
                {
                    if (ite.Value.Data == qrCodeData)
                    {
                        return ite.Key;
                    }
                }
            }
            return new Guid();
        }

        public IList<QRCode> GetList()
        {
            lock (qrCodesList)
            {
                return new List<QRCode>(qrCodesList.Values);
            }
        }

        private void Awake()
        {
            IsWatcherRunning = false;
        }

        protected async void Start()
        {
            if (AutoStartQRWatching)
            {
                await StartQRWatchingAsync();
            }
        }

        public Task<QRCodeWatcherAccessStatus> StartQRWatchingAsync(CancellationToken cancellationToken = default)
        {
            lock (lockObj)
            {
                if (startWatcherTask != null)
                {
                    DebugLog("Returning existing start tracker task");
                    return startWatcherTask;
                }

                stopTrackerTask = null;
                startWatcherCTS = new CancellationTokenSource();

                var hybridCTS = CancellationTokenSource.CreateLinkedTokenSource(startWatcherCTS.Token, cancellationToken);
                DebugLog("Kicking off a new start tracker task");
                return startWatcherTask = Task.Run(() => StartQRWatchingAsyncImpl(hybridCTS.Token), hybridCTS.Token);
            }
        }

        private async Task<QRCodeWatcherAccessStatus> StartQRWatchingAsyncImpl(CancellationToken token)
        {
            QRCodeWatcherAccessStatus accessStatus = QRCodeWatcherAccessStatus.DeniedBySystem;

#if WINDOWS_UWP
            DebugLog("Requesting QRCodeWatcher capability");
            accessStatus = await QRCodeWatcher.RequestAccessAsync();
            if (accessStatus != QRCodeWatcherAccessStatus.Allowed)
            {
                DebugLog("Failed to obtain QRCodeWatcher capability. QR Codes will not be detected");
            }
            else
            {
                DebugLog("QRCodeWatcher capability granted.");
            }
#endif

            if (accessStatus == QRCodeWatcherAccessStatus.Allowed)
            {
                // Note: If the QRCodeWatcher is created prior to obtaining the QRCodeWatcher capability, initialization will fail.
                if (qrWatcher == null)
                {
                    DebugLog("Creating qr tracker");
                    qrWatcher = new QRCodeWatcher();
                    qrWatcher.Added += QRWatcherAdded;
                    qrWatcher.Updated += QRWatcherUpdated;
                    qrWatcher.Removed += QRWatcherRemoved;
                }

                if (!token.IsCancellationRequested &&
                    !IsWatcherRunning)
                {
                    qrWatcher.Start();
                    IsWatcherRunning = true;
                }
            }

            return await Task.FromResult(accessStatus);
        }

        public Task StopQRWatchingAsync()
        {
            lock (lockObj)
            {
                if (startWatcherTask == null)
                {
                    DebugLog("StopQRTrackerAsync was called when no start task had been created.");
                    return Task.CompletedTask;
                }

                if (stopTrackerTask != null)
                {
                    DebugLog("StopQRTrackerAsync was called when already stopping tracking.");
                    return stopTrackerTask;
                }

                startWatcherCTS.Cancel();
                startWatcherCTS.Dispose();
                startWatcherCTS = null;

                DebugLog("Stop tracker task created.");
                return stopTrackerTask = Task.Run(() => StopQRWatchingAsyncImpl(startWatcherTask));
            }
        }


        private async Task StopQRWatchingAsyncImpl(Task previousTask)
        {
            await previousTask.IgnoreCancellation();

            if (qrWatcher != null &&
                IsWatcherRunning)
            {
                qrWatcher.Stop();
                IsWatcherRunning = false;
                DebugLog("QR tracker was stopped.");

                lock (qrCodesList)
                {
                    qrCodesList.Clear();
                    DebugLog("QR Code list was cleared when stopping.");
                }
            }

            lock (lockObj)
            {
                startWatcherTask = null;
                DebugLog("Start tracker task was set back to null.");
            }
        }

        private void QRWatcherRemoved(object sender, QRCodeRemovedEventArgs args)
        {
            lock (qrCodesList)
            {
                qrCodesList.Remove(args.Code.Id);
            }

            Debug.Log("QR Code Lost: " + args.Code.Data);
            QRCodeRemoved?.Invoke(this, args.Code);
        }

        private void QRWatcherUpdated(object sender, QRCodeUpdatedEventArgs args)
        {
            lock (qrCodesList)
            {
                if (!qrCodesList.ContainsKey(args.Code.Id))
                {
                    Debug.LogWarning($"QRCode updated that was not previously being observed: {args.Code.Data}");
                }

                qrCodesList[args.Code.Id] = args.Code;
            }

            Debug.Log("QR Code Updated: " + args.Code.Data);
            QRCodeUpdated?.Invoke(this, args.Code);
        }

        private void QRWatcherAdded(object sender, QRCodeAddedEventArgs args)
        {
            lock (qrCodesList)
            {
                qrCodesList[args.Code.Id] = args.Code;
            }

            Debug.Log("QR Code Added: " + args.Code.Data);
            QRCodeAdded?.Invoke(this, args.Code);
        }

        private void DebugLog(string message)
        {
            if (DebugLogging)
            {
                Debug.Log($"QRCodesManager: {message}");
            }
        }
    }
}
#endif