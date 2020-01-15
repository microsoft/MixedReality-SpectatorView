// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP && UNITY_WSA
#define ENABLE_QRCODES
#endif

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_QRCODES
using Microsoft.MixedReality.QR;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// QR code detector that implements <see cref="Microsoft.MixedReality.SpectatorView.IMarkerDetector"/>
    /// </summary>
    public class QRCodeMarkerDetector : MonoBehaviour,
        IMarkerDetector
    {
        [Tooltip("Check to enable debug logging")]
        [SerializeField]
        private bool debugLogging = false;

#if ENABLE_QRCODES
        private QRCodesManager _qrCodesManager;
        private Dictionary<QRCode, SpatialCoordinateSystem> _markerCoordinateSystems = new Dictionary<QRCode, SpatialCoordinateSystem>();
        private bool _processMarkers = false;
        private Dictionary<QRCode, int> _markerIds = new Dictionary<QRCode, int>();
#endif

        private object _contentLock = new object();
        private Dictionary<int, float> _markerSizes = new Dictionary<int, float>();
        private readonly string _qrCodeNamePrefix = "sv";

#if ENABLE_QRCODES
        private bool _tracking = false;
#endif

#pragma warning disable 67
        /// <inheritdoc />
        public event MarkersUpdatedHandler MarkersUpdated;
#pragma warning restore 67

        /// <inheritdoc />
        public void SetMarkerSize(float markerSize){}

        /// <inheritdoc />
        public MarkerPositionBehavior MarkerPositionBehavior { get; set; }

        /// <inheritdoc />
        public void StartDetecting()
        {
            enabled = true;

#if ENABLE_QRCODES
            TrimMarkers();
            _tracking = true;
            _processMarkers = true;
#else
            Debug.LogError("Current platform does not support qr code marker detector");
#endif
        }

        /// <inheritdoc />
        public void StopDetecting()
        {
#if ENABLE_QRCODES
            _tracking = false;
            _processMarkers = false;
#else
            Debug.LogError("Current platform does not support qr code marker detector");
#endif

            enabled = false;
        }

        /// <inheritdoc />
        public bool TryGetMarkerSize(int markerId, out float size)
        {
            lock(_contentLock)
            {
                if (_markerSizes.TryGetValue(markerId, out size))
                {
                    return true;
                }
            }

            size = 0.0f;
            return false;
        }

        protected void Update()
        {
#if ENABLE_QRCODES
            if (_tracking &&
                _processMarkers)
            {
                ProcessMarkerUpdates();
            }
#endif
        }

#if ENABLE_QRCODES
        protected async void OnEnable()
        {
            if (_qrCodesManager == null)
            {
                _qrCodesManager = QRCodesManager.FindOrCreateQRCodesManager(gameObject);
                _qrCodesManager.DebugLogging = debugLogging;
            }

            if (_qrCodesManager != null)
            {
                _qrCodesManager.QRCodeAdded += QRCodeAdded;
                _qrCodesManager.QRCodeRemoved += QRCodeRemoved;
                _qrCodesManager.QRCodeUpdated += QRCodeUpdated;
                await StartTrackingAsync();
            }
        }

        protected async void OnDestroy()
        {
            if (_qrCodesManager != null)
            {
                await StopTrackingAsync();
                _qrCodesManager.QRCodeAdded -= QRCodeAdded;
                _qrCodesManager.QRCodeRemoved -= QRCodeRemoved;
                _qrCodesManager.QRCodeUpdated -= QRCodeUpdated;
            }
        }

        private async Task StartTrackingAsync()
        {
            var result = await _qrCodesManager.StartQRWatchingAsync();
            DebugLog($"Started qr watcher: {result.ToString()}");
        }

        private async Task StopTrackingAsync()
        {
            await _qrCodesManager.StopQRWatchingAsync();
            DebugLog("Stopped qr watcher");
        }

        private void QRCodeAdded(object sender, QRCode qrCode)
        {
            if (TryGetMarkerId(qrCode.Data, out var markerId))
            {
                lock (_contentLock)
                {
                    _markerIds[qrCode] = markerId;
                    _markerSizes[markerId] = qrCode.PhysicalSideLength;
                    _processMarkers = true;
                }
            }
        }

        private void QRCodeUpdated(object sender, QRCode qrCode)
        {
            if (TryGetMarkerId(qrCode.Data, out var markerId))
            {
                lock (_contentLock)
                {
                    _markerIds[qrCode] = markerId;
                    _markerSizes[markerId] = qrCode.PhysicalSideLength;
                    _processMarkers = true;
                }
            }
        }

        private void QRCodeRemoved(object sender, QRCode qrCode)
        {
            lock (_contentLock)
            {
                if (_markerIds.TryGetValue(qrCode, out var markerId))
                {
                    _markerSizes.Remove(markerId);
                }

                _markerIds.Remove(qrCode);
                _markerCoordinateSystems.Remove(qrCode);
                _processMarkers = true;
            }
        }

        private void ProcessMarkerUpdates()
        {
            bool locatedAllMarkers = true;
            var markerDictionary = new Dictionary<int, Marker>();
            lock (_contentLock)
            {
                foreach (var markerPair in _markerIds)
                {
                    if (!_markerCoordinateSystems.ContainsKey(markerPair.Key))
                    {
                        var coordinateSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(markerPair.Key.SpatialGraphNodeId);
                        if (coordinateSystem != null)
                        {
                            _markerCoordinateSystems[markerPair.Key] = coordinateSystem;
                        }
                    }
                }

                foreach (var coordinatePair in _markerCoordinateSystems)
                {
                    if (!_markerIds.TryGetValue(coordinatePair.Key, out var markerId))
                    {
                        DebugLog($"Failed to locate marker:{coordinatePair.Key}, {markerId}");
                        locatedAllMarkers = false;
                        continue;
                    }

                    if (_qrCodesManager.TryGetLocationForQRCode(coordinatePair.Value, out var location))
                    {
                        var translation = location.GetColumn(3);
                        // The obtained QRCode orientation will reflect a positive y axis down the QRCode.
                        // Spectator view marker detectors should return a positive y axis up the marker,
                        // so, we rotate the marker orientation 180 degrees around its z axis.
                        var rotation = Quaternion.LookRotation(location.GetColumn(2), location.GetColumn(1)) * Quaternion.Euler(0, 0, 180);

                        if (_markerSizes.TryGetValue(markerId, out var size))
                        {
                            var transform = Matrix4x4.TRS(translation, rotation, Vector3.one);
                            var offset = -1.0f * size / 2.0f;
                            var markerCenter = transform.MultiplyPoint(new Vector3(offset, offset, 0));
                            var marker = new Marker(markerId, markerCenter, rotation);
                            markerDictionary[markerId] = marker;
                        }
                    }
                }
            }

            if (markerDictionary.Count > 0 || locatedAllMarkers)
            {
                MarkersUpdated?.Invoke(markerDictionary);
            }

            // Stop processing markers once all markers have been located
            _processMarkers = !locatedAllMarkers;
        }
#endif // ENABLE_QRCODES

        private void TrimMarkers()
        {
#if ENABLE_QRCODES
            lock (_contentLock)
            {
                long currTime = System.Diagnostics.Stopwatch.GetTimestamp();
                List<QRCode> keysToRemove = new List<QRCode>();
                foreach (var markerPair in _markerIds)
                {
                    if (markerPair.Key.SystemRelativeLastDetectedTime.Ticks < currTime)
                    {
                        keysToRemove.Add(markerPair.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    DebugLog($"Removing QRCode based on old detection timestamp: {key.Data}");
                    if (_markerIds.TryGetValue(key, out int id))
                    {
                        _markerSizes.Remove(id);
                    }

                    _markerIds.Remove(key);
                    _markerCoordinateSystems.Remove(key);
                }
            }
#endif
        }

        private bool TryGetMarkerId(string qrCode, out int markerId)
        {
            markerId = -1;
            if (qrCode != null &&
                qrCode.Trim().StartsWith(_qrCodeNamePrefix))
            {
                var qrCodeId = qrCode.Trim().Replace(_qrCodeNamePrefix, "");
                if (Int32.TryParse(qrCodeId, out markerId))
                {
                    return true;
                }
            }

            DebugLog($"Unable to obtain markerId for QR code: {qrCode}");
            markerId = -1;
            return false;
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"QRCodeMarkerDetector: {message}");
            }
        }
    }
}
