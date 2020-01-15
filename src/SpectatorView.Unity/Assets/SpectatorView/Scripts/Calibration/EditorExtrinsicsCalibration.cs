// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class EditorExtrinsicsCalibration : MonoBehaviour
    {
        [Header("Camera Intrinsics")]
        /// <summary>
        /// Path to a camera intrinsics file created using chessboard intrinsics calibration.
        /// </summary>
        [Tooltip("Path to a camera intrinsics file created using chessboard intrinsics calibration.")]
        [SerializeField]
        protected string cameraIntrinsicsPath = "";

        [Header("Physical Calibration Board Parameters")]
        /// <summary>
        /// The minimum number of ArUco markers detected in a sample set to use in processing.
        /// </summary>
        [Tooltip("The minimum number of ArUco markers detected in a sample set to use in processing.")]
        [SerializeField]
        public int MinimumNumberOfDetectedMarkers = 5;

        [Header("HoloLens Parameters")]
        /// <summary>
        /// The HolographicCameraObserver that establishes a network connection with the Holographic Camera.
        /// </summary>
        [Tooltip("The HolographicCameraObserver that establishes a network connection with the Holographic Camera.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        HolographicCameraObserver holographicCameraObserver = null;
#pragma warning restore 414

        [Header("UI Parameters")]
        /// <summary>
        /// Image for displaying the dslr camera feed.
        /// </summary>
        [Tooltip("Image for displaying the dslr camera feed.")]
        [SerializeField]
        protected RawImage feedImage;

        /// <summary>
        /// Image for displaying the last processed ArUco marker dataset.
        /// </summary>
        [Tooltip(" Image for displaying the last processed ArUco marker dataset.")]
        [SerializeField]
        protected RawImage lastArUcoImage;

        /// <summary>
        /// Used to draw debug visuals for detected aruco markers.
        /// </summary>
        [Tooltip("Used to draw debug visuals for detected aruco markers.")]
        [SerializeField]
        protected DebugVisualHelper markerVisualHelper;

        /// <summary>
        /// Used to draw debug visuals for camera positions/orientations.
        /// </summary>
        [Tooltip("Used to draw debug visuals for camera positions/orientations.")]
        [SerializeField]
        protected DebugVisualHelper cameraVisualHelper;

        /// <summary>
        /// The number of datasets that have been successfully processed.s
        /// </summary>
        public int ProcessedDatasetCount => processedDatasetCount;
        
        /// <summary>
        /// The number of markers detected in the last dataset.
        /// </summary>
        public int LastDetectedMarkersCount => lastDetectedMarkersCount;
        
        /// <summary>
        /// The output camera extrinsics calculated from all usable datasets.
        /// </summary>
        public CalculatedCameraExtrinsics GlobalExtrinsics => globalExtrinsics;
        
        /// <summary>
        /// The file name for the output camera extrinsics calculated from all usable datasets.
        /// </summary>
        public string GlobalExtrinsicsFileName => globalExtrinsicsFileName;
        
        /// <summary>
        /// The file name for the found calibration data. Calibration data includes both camera intrinsics and extrinsics.
        /// </summary>
        public string CalibrationFileName => calibrationFileName;
        
        /// <summary>
        /// A flag indicating whether the last attempt at uploading calibration data to a connected HoloLens device succeeded.
        /// </summary>
        public bool UploadSucceeded => uploadSucceeded;

        /// <summary>
        /// A message associated with the last attempt to upload calibration data to a connected HoloLens device.
        /// </summary>
        public string UploadResultMessage => uploadResultMessage;

        private CalculatedCameraIntrinsics dslrIntrinsics;
        private List<CalculatedCameraExtrinsics> cameraExtrinsics;
        private CalculatedCameraExtrinsics globalExtrinsics = null;
        private string globalExtrinsicsFileName = string.Empty;
        private List<GameObject> parentVisuals = new List<GameObject>();
        private int processedDatasetCount = 0;
        private int lastDetectedMarkersCount = 0;
        private string calibrationFileName = string.Empty;
        private CalculatedCameraCalibration lastCalibration;
        private string uploadResultMessage = string.Empty;
        private bool uploadSucceeded = false;

#if UNITY_EDITOR
        private HeadsetCalibrationData headsetData = null;

        private void Start()
        {
            holographicCameraObserver.RegisterCommandHandler(HeadsetCalibration.CalibrationDataReceivedCommandHeader, OnCalibrationDataReceived);
            holographicCameraObserver.RegisterCommandHandler(HeadsetCalibration.UploadCalibrationResultCommandHeader, OnCalibrationResultReceived);
            CalibrationAPI.Instance.Reset();
            CalibrationDataHelper.Initialize();
            dslrIntrinsics = CalibrationDataHelper.LoadCameraIntrinsics(cameraIntrinsicsPath);
            if (dslrIntrinsics == null)
            {
                throw new Exception("Failed to load the camera intrinsics file.");
            }
            else
            {
                Debug.Log($"Successfully loaded the provided camera intrinsics file: {dslrIntrinsics}");
            }

            var arucoDatasetFileNames = CalibrationDataHelper.GetArUcoDatasetFileNames();
            foreach (var fileName in arucoDatasetFileNames)
            {
                var dslrTexture = CalibrationDataHelper.LoadDSLRArUcoImage(fileName);
                var headsetData = CalibrationDataHelper.LoadHeadsetData(fileName);

                if (dslrTexture == null ||
                    headsetData == null)
                {
                    Debug.LogWarning($"Failed to locate dataset: {fileName}");
                }
                else if (!ProcessArUcoData(headsetData, dslrTexture))
                {
                    Debug.LogWarning($"Failed to process dataset: {fileName}");
                }
                else
                {
                    processedDatasetCount++;
                    CalibrationDataHelper.SaveDSLRArUcoDetectedImage(dslrTexture, fileName);
                    CreateVisual(headsetData, fileName);
                }
            }
        }

        private void Update()
        {
            if (feedImage != null &&
                feedImage.texture == null)
            {
                feedImage.texture = CompositorWrapper.Instance.GetVideoCameraFeed();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                RequestHeadsetData();
            }

            if (headsetData != null)
            {
                lastDetectedMarkersCount = headsetData.markers.Count;
                if (headsetData.markers.Count < MinimumNumberOfDetectedMarkers)
                {
                    Debug.Log("Data set did not contain enough markers to use.");
                }
                else
                {
                    var dslrTexture = CompositorWrapper.Instance.GetVideoCameraTexture();
                    var fileName = CalibrationDataHelper.GetUniqueFileName();
                    CalibrationDataHelper.SaveDSLRArUcoImage(dslrTexture, fileName);
                    CalibrationDataHelper.SaveHeadsetData(headsetData, fileName);

                    if (ProcessArUcoData(headsetData, dslrTexture))
                    {
                        processedDatasetCount++;
                        CalibrationDataHelper.SaveDSLRArUcoDetectedImage(dslrTexture, fileName);
                        CreateVisual(headsetData, fileName);
                    }
                }

                headsetData = null;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                CalculateExtrinsics();
            }
        }

        /// <summary>
        /// Call to request another dataset from the connected HoloLens device.
        /// </summary>
        public void RequestHeadsetData()
        {
            if (holographicCameraObserver != null &&
                holographicCameraObserver.IsConnected)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(HeadsetCalibration.RequestCalibrationDataCommandHeader);

                    var request = new HeadsetCalibrationDataRequest();
                    request.timestamp = Time.time;
                    request.SerializeAndWrite(writer);

                    writer.Flush();
                    holographicCameraObserver.Broadcast(memoryStream.GetBuffer(), 0, memoryStream.Position);
                }
            }
            else
            {
                Debug.LogWarning("HolographicCameraObserver isn't setup correctly, failed to request headset data.");
            }
        }

        /// <summary>
        /// Call to calculate camera extrinsics based on all obtained and usable datasets.
        /// </summary>
        public void CalculateExtrinsics()
        {
            if (processedDatasetCount > 0)
            {
                Debug.Log("Starting Individual Camera Extrinsics calculations.");
                cameraExtrinsics = CalibrationAPI.Instance.CalculateIndividualArUcoExtrinsics(dslrIntrinsics, parentVisuals.Count);
                if (cameraExtrinsics != null)
                {
                    CreateExtrinsicsVisual(cameraExtrinsics);
                }
                Debug.Log("Completed Individual Camera Extrinsics calculations.");

                Debug.Log("Starting the Global Camera Extrinsics calculation.");
                globalExtrinsics = CalibrationAPI.Instance.CalculateGlobalArUcoExtrinsics(dslrIntrinsics);
                if (globalExtrinsics != null)
                {
                    globalExtrinsicsFileName = CalibrationDataHelper.SaveCameraExtrinsics(globalExtrinsics);
                    Debug.Log($"Saved global extrinsics: {globalExtrinsicsFileName}");
                    Debug.Log($"Found global extrinsics: {globalExtrinsics}");
                    var position = globalExtrinsics.ViewFromWorld.GetColumn(3);
                    var rotation = Quaternion.LookRotation(globalExtrinsics.ViewFromWorld.GetColumn(2), globalExtrinsics.ViewFromWorld.GetColumn(1));
                    GameObject camera = null;
                    cameraVisualHelper.CreateOrUpdateVisual(ref camera, position, rotation);
                    camera.name = "Global Extrinsics";
                    GameObject hololens = null;
                    cameraVisualHelper.CreateOrUpdateVisual(ref hololens, Vector3.zero, Quaternion.identity);
                    hololens.name = "Global HoloLens";

                    lastCalibration = new CalculatedCameraCalibration(dslrIntrinsics, globalExtrinsics);
                    calibrationFileName = CalibrationDataHelper.SaveCameraCalibration(lastCalibration);
                }
            }
            else
            {
                Debug.LogWarning("No usable marker datasets have been processed, unable to calculate camera extrinsics.");
            }
        }

        /// <summary>
        /// Call to attempt uploading the last calculated calibration data to a connected HoloLens device.
        /// </summary>
        public void UploadCalibrationData()
        {
            uploadResultMessage = string.Empty;
            if (lastCalibration == null)
            {
                Debug.LogWarning("Calibration isn't currently loaded, failed to upload calibration data");
                return;
            }

            if (holographicCameraObserver != null &&
                holographicCameraObserver.IsConnected)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(HeadsetCalibration.UploadCalibrationCommandHeader);
                    var payload = lastCalibration.Serialize();
                    writer.Write(payload.Length);
                    writer.Write(payload);
                    writer.Flush();
                    holographicCameraObserver.Broadcast(memoryStream.GetBuffer(), 0, memoryStream.Position);
                    Debug.Log("Sent calibration data to the hololens device.");
                }
            }
            else
            {
                Debug.LogWarning("HolographicCameraObserver isn't setup correctly, failed to request headset data.");
            }
        }

        private void OnCalibrationDataReceived(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            Debug.Log("Received calibration data payload.");
            HeadsetCalibrationData headsetCalibrationData;
            if (HeadsetCalibrationData.TryDeserialize(reader, out headsetCalibrationData))
            {
                headsetData = headsetCalibrationData;
            }
        }

        private void OnCalibrationResultReceived(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            uploadSucceeded = reader.ReadBoolean();
            uploadResultMessage = reader.ReadString();
        }

        private bool ProcessArUcoData(HeadsetCalibrationData headsetData, Texture2D dslrTexture)
        {
            if (dslrTexture == null ||
                dslrTexture.format != TextureFormat.RGB24)
            {
                return false;
            }

            int imageWidth = dslrTexture.width;
            int imageHeight = dslrTexture.height;
            var unityPixels = dslrTexture.GetRawTextureData<byte>();
            var pixels = unityPixels.ToArray();

            if (!CalibrationAPI.Instance.ProcessArUcoData(headsetData, pixels, imageWidth, imageHeight))
            {
                return false;
            }

            for (int i = 0; i < unityPixels.Length; i++)
            {
                unityPixels[i] = pixels[i];
            }

            dslrTexture.Apply();

            if (lastArUcoImage)
                lastArUcoImage.texture = dslrTexture;

            return true;
        }

        private void CreateVisual(HeadsetCalibrationData data, string fileName)
        {
            var parent = new GameObject();
            parent.name = $"Dataset {fileName}";

            var inScene = new GameObject();
            inScene.name = $"Objects in scene position";
            inScene.transform.parent = parent.transform;

            for (int i = 0; i < data.markers.Count; i++)
            {
                GameObject temp = null;
                var corners = data.markers[i].arucoMarkerCorners;
                float dist = Vector3.Distance(corners.topLeft, corners.topRight);
                markerVisualHelper.CreateOrUpdateVisual(ref temp, corners.topLeft, corners.orientation, dist * Vector3.one);
                temp.name = $"Marker {fileName}.{data.markers[i].id}";
                temp.transform.parent = inScene.transform;
            }

            GameObject camera = null;
            cameraVisualHelper.CreateOrUpdateVisual(ref camera, data.headsetData.position, data.headsetData.rotation);
            camera.name = $"HoloLens {fileName}";
            camera.transform.parent = inScene.transform;

            var origin = new GameObject();
            origin.name = $"Objects adjusted to origin";
            origin.transform.parent = parent.transform;

            GameObject originCamera = null;
            cameraVisualHelper.CreateOrUpdateVisual(ref originCamera, Vector3.zero, Quaternion.identity);
            originCamera.name = $"HoloLens {fileName}";
            originCamera.transform.parent = origin.transform;

            var allCorners = CalibrationAPI.CalcMarkerCornersRelativeToCamera(data);
            for (int i = 0; i < allCorners.Count; i++)
            {
                GameObject temp = null;
                var corners = allCorners[i];
                float dist = Vector3.Distance(corners.topLeft, corners.topRight);
                markerVisualHelper.CreateOrUpdateVisual(ref temp, corners.topLeft, corners.orientation, dist * Vector3.one);
                temp.name = $"Marker {fileName}.{data.markers[i].id}";
                temp.transform.parent = origin.transform;
            }

            parentVisuals.Add(parent);
        }

        private void CreateExtrinsicsVisual(List<CalculatedCameraExtrinsics> extrinsics)
        {
            if (extrinsics.Count < parentVisuals.Count)
            {
                Debug.LogWarning("Extrinsics count should be at least as large as the parent visuals count, visuals not created");
            }

            for (int i = 0; i < parentVisuals.Count; i++)
            {
                var parent = parentVisuals[i];
                GameObject camera = null;
                var extrinsic = extrinsics[i];
                var position = extrinsic.ViewFromWorld.GetColumn(3);
                var rotation = Quaternion.LookRotation(extrinsic.ViewFromWorld.GetColumn(2), extrinsic.ViewFromWorld.GetColumn(1));
                cameraVisualHelper.CreateOrUpdateVisual(ref camera, position, rotation);
                camera.name = "Calculated DSLR";
                camera.transform.parent = parent.transform;
            }
        }
#endif
    }
}
