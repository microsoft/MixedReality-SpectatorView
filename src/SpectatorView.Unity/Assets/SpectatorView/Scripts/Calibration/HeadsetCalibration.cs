// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Called when HeadsetCalibration has a new qr code/aruco marker payload
    /// </summary>
    /// <param name="data">byte data to send over the network</param>
    public delegate void HeadsetCalibrationDataUpdatedHandler(HeadsetCalibrationData data);

    public class HeadsetCalibration : MonoBehaviour
    {
        /// <summary>
        /// Check to show debug visuals for the detected markers.
        /// </summary>
        [Tooltip("Check to show debug visuals for the detected markers.")]
        [SerializeField]
        protected bool showDebugVisuals = true;

        /// <summary>
        /// QR Code Marker Detector in scene
        /// </summary>
        [Tooltip("QR Code Marker Detector in scene")]
        [SerializeField]
        protected QRCodeMarkerDetector qrCodeMarkerDetector;

        /// <summary>
        /// ArUco Code Marker Detector in scene
        /// </summary>
        [Tooltip("ArUco Code Marker Detector in scene")]
        [SerializeField]
        protected ArUcoMarkerDetector arucoCodeMarkerDetector;

        /// <summary>
        /// Debug Visual Helper in scene that will place game objects on qr code markers in the scene.
        /// </summary>
        [Tooltip("Debug Visual Helper in scene that will place game objects on qr code markers in the scene.")]
        [SerializeField]
        protected DebugVisualHelper qrCodeDebugVisualHelper;

        /// <summary>
        /// Debug Visual Helper in scene that will place game objects on aruco markers in the scene.
        /// </summary>
        [Tooltip("Debug Visual Helper in scene that will place game objects on aruco markers in the scene.")]
        [SerializeField]
        protected DebugVisualHelper arucoDebugVisualHelper;

        public static readonly string RequestCalibrationDataCommandHeader = "REQCALIBDATA";
        public static readonly string CalibrationDataReceivedCommandHeader = "CALIBDATA";
        public static readonly string UploadCalibrationCommandHeader= "UPLOADCALIBDATA";
        public static readonly string UploadCalibrationResultCommandHeader = "UPLOADCALIBRESULT";

        private IMarkerDetector markerDetector = null;
        private bool markersUpdated = false;
        private Dictionary<int, Marker> markers = new Dictionary<int, Marker>();
        private Dictionary<int, GameObject> qrCodeDebugVisuals = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> arucoDebugVisuals = new Dictionary<int, GameObject>();
        private readonly float markerPaddingRatio = 34f / (300f - (2f * 34f)); // padding pixels / marker width in pixels - This is based off of the output from CalibrationBoardGenerator.exe
        private Dictionary<int, MarkerPair> markerPairs = new Dictionary<int, MarkerPair>();
        private ConcurrentQueue<HeadsetCalibrationData> sendQueue = new ConcurrentQueue<HeadsetCalibrationData>();

        /// <inheritdoc />
        public event HeadsetCalibrationDataUpdatedHandler Updated;

        /// <summary>
        /// Call to signal to the HeadsetCalibration class that it should create a new qr code/aruco marker payload
        /// </summary>
        public void UpdateHeadsetCalibrationData()
        {
            Debug.Log("Updating headset calibration data");
            var data = new HeadsetCalibrationData();
            data.timestamp = Time.time;
            data.headsetData.position = Camera.main.transform.position;
            data.headsetData.rotation = Camera.main.transform.rotation;
            data.markers = new List<MarkerPair>();
            foreach (var qrCodePair in markers)
            {
                if (markerPairs.ContainsKey(qrCodePair.Key))
                {
                    var markerPair = markerPairs[qrCodePair.Key];
                    data.markers.Add(markerPair);
                }
            }

            sendQueue.Enqueue(data);
        }

        private void OnEnable()
        {
            if (!(qrCodeMarkerDetector is QRCodeMarkerDetector))
            {
                Debug.LogError("HeadsetCalibration is missing a valid QRCodeMarkerDetector");
            }
            if (!(arucoCodeMarkerDetector is ArUcoMarkerDetector))
            {
                Debug.LogError("HeadsetCalibration is missing a valid ArUcoMarkerDetector");
            }

            var detector = Application.platform == RuntimePlatform.WSAPlayerX86
                ? arucoCodeMarkerDetector as MonoBehaviour
                : qrCodeMarkerDetector as MonoBehaviour;
            detector.gameObject.SetActive(true);
            markerDetector = detector as IMarkerDetector;

            markerDetector.MarkersUpdated += OnMarkersUpdated;
            markerDetector.StartDetecting();
        }

        private void OnDisable()
        {
            markerDetector.StopDetecting();
            markerDetector.MarkersUpdated -= OnMarkersUpdated;
        }

        private void Update()
        {
            if (markersUpdated)
            {
                markersUpdated = false;
                ProcessMarkerUpdate();
            }

            while (sendQueue.Count > 0)
            {
                if (sendQueue.TryDequeue(out var data))
                {
                    Updated?.Invoke(data);
                }
            }
        }

        private void OnMarkersUpdated(Dictionary<int, Marker> updatedMarkers)
        {
            MergeDictionaries(markers, updatedMarkers);
            markersUpdated = true;
        }

        private void ProcessMarkerUpdate()
        {
            HashSet<int> updatedMarkerIds = new HashSet<int>();

            foreach (var marker in markers)
            {
                updatedMarkerIds.Add(marker.Key);
                float size = 0;
                if (markerDetector.TryGetMarkerSize(marker.Key, out size))
                {
                    var markerTopLeftPosition = CalcTopLeftFromCenter(marker.Value.Position, marker.Value.Rotation, size);
                    var markerRotation = marker.Value.Rotation;

                    if (showDebugVisuals)
                    {
                        GameObject qrCodeDebugVisual = null;
                        qrCodeDebugVisuals.TryGetValue(marker.Key, out qrCodeDebugVisual);
                        qrCodeDebugVisualHelper.CreateOrUpdateVisual(ref qrCodeDebugVisual, markerTopLeftPosition, markerRotation, size * Vector3.one);
                        qrCodeDebugVisuals[marker.Key] = qrCodeDebugVisual;
                    }

                    Vector3 arucoTopLeftPosition;
                    if (markerDetector is ArUcoMarkerDetector)
                        arucoTopLeftPosition = markerTopLeftPosition;
                    else
                    {
                        var originToQRCode = Matrix4x4.TRS(markerTopLeftPosition, markerRotation, Vector3.one);
                        arucoTopLeftPosition = originToQRCode.MultiplyPoint(new Vector3(-1.0f * ((2.0f * (size * markerPaddingRatio)) + (size)), 0, 0));
                    }
                    // We assume that the aruco marker has the same orientation as the qr code marker because they are on the same plane/2d calibration board.
                    var arucoRotation = marker.Value.Rotation;

                    if (showDebugVisuals)
                    {
                        GameObject arucoDebugVisual = null;
                        arucoDebugVisuals.TryGetValue(marker.Key, out arucoDebugVisual);
                        arucoDebugVisualHelper.CreateOrUpdateVisual(ref arucoDebugVisual, arucoTopLeftPosition, arucoRotation, size * Vector3.one);
                        arucoDebugVisuals[marker.Key] = arucoDebugVisual;
                    }

                    var markerPair = new MarkerPair();
                    markerPair.id = marker.Key;
                    markerPair.qrCodeMarkerCorners = CalculateMarkerCorners(markerTopLeftPosition, markerRotation, size);
                    markerPair.arucoMarkerCorners = CalculateMarkerCorners(arucoTopLeftPosition, arucoRotation, size);

                    lock (markerPairs)
                    {
                        markerPairs[marker.Key] = markerPair;
                    }
                }
            }

            RemoveUnobservedItemsAndDestroy(qrCodeDebugVisuals, updatedMarkerIds);
            RemoveUnobservedItemsAndDestroy(arucoDebugVisuals, updatedMarkerIds);
        }

        private static void MergeDictionaries(Dictionary<int, Marker> dictionary, Dictionary<int, Marker> update)
        {
            HashSet<int> observedMarkers = new HashSet<int>();
            foreach (var markerUpdate in update)
            {
                dictionary[markerUpdate.Key] = markerUpdate.Value;
                observedMarkers.Add(markerUpdate.Key);
            }

            RemoveUnobservedItems(dictionary, observedMarkers);
        }

        private static void RemoveUnobservedItems<TKey, TValue>(Dictionary<TKey, TValue> items, HashSet<TKey> itemsToKeep)
        {
            List<TKey> keysToRemove = new List<TKey>();
            foreach (var pair in items)
            {
                if (!itemsToKeep.Contains(pair.Key))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                items.Remove(key);
            }
        }

        private static void RemoveUnobservedItemsAndDestroy<TKey>(Dictionary<TKey, GameObject> items, HashSet<TKey> itemsToKeep)
        {
            List<TKey> keysToRemove = new List<TKey>();
            foreach (var pair in items)
            {
                if (!itemsToKeep.Contains(pair.Key))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Debug.Log($"Destroying debug visual for marker id:{key}");
                var visual = items[key];
                items.Remove(key);
                Destroy(visual);
            }
        }

        private static Vector4 GetPosition(Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

        private static Quaternion GetRotation(Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }

        private static Vector3 CalcTopLeftFromCenter(Vector3 middlePosition, Quaternion orientation, float size)
        {
            var originToMiddle = Matrix4x4.TRS(middlePosition, orientation, Vector3.one);
            return originToMiddle.MultiplyPoint(new Vector3(size / 2.0f, size / 2.0f, 0));
        }

        private static MarkerCorners CalculateMarkerCorners(Vector3 topLeftPosition, Quaternion topLeftOrientation, float size)
        {
            var corners = new MarkerCorners();
            corners.topLeft = topLeftPosition;
            var originToTopLeftCorner = Matrix4x4.TRS(topLeftPosition, topLeftOrientation, Vector3.one);
            corners.topRight = originToTopLeftCorner.MultiplyPoint(new Vector3(-size, 0, 0));
            corners.bottomLeft = originToTopLeftCorner.MultiplyPoint(new Vector3(0, -size, 0));
            corners.bottomRight = originToTopLeftCorner.MultiplyPoint(new Vector3(-size, -size, 0));
            corners.orientation = topLeftOrientation;
            return corners;
        }
    }
}
