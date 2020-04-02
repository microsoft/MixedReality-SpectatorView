// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once


#if defined(INCLUDE_AZUREKINECT)

#include "ArUcoMarkerDetector.h"
#include "AzureKinectCameraFrame.h"
#include <thread>
#include <opencv2\aruco.hpp>
#include <k4a/k4a.h>
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
#include <k4abt.h>
#endif
#define MAX_NUM_CACHED_BUFFERS 20

// Reads and buffers input from the Azure Kinect camera into a circular buffer.
// The input threads stage AzureKinectCameraFrames, which contain buffered copies
// of the color, depth, and body index images for that frame.
// The output thread calls UpdateSRVs to read staged frames and write the results
// into the SRVs used by the compositor.
class AzureKinectCameraInput
{
public:
    AzureKinectCameraInput(k4a_depth_mode_t depthMode, bool captureDepth, bool captureBodyMask);
    ~AzureKinectCameraInput();

    int GetCaptureFrameIndex()
    {
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
        if (_captureBodyMask)
        {
            return _currentBodyMaskFrameIndex;
        }
#endif
        return _currentFrameIndex;
    }
    void GetCameraCalibrationInformation(CameraIntrinsics* calibration);
    void StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize);
    void StopArUcoMarkerDetector();
    int GetLatestArUcoMarkerCount() { return markerDetector->GetDetectedMarkersCount(); }
    void GetLatestArUcoMarkers(int size, Marker* markers);

    bool UpdateSRVs(int frameIndex, ID3D11Device* device, ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV);

private:
    void RunCaptureLoop();
    void UpdateArUcoMarkers(k4a_image_t image);

    std::atomic_bool _captureDepth;
    std::atomic_bool _captureBodyMask;
    k4a_device_t k4aDevice;
    k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
    k4a_calibration_t calibration;
    k4a_transformation_t transformation;
    k4a_image_t transformedDepthImage;
    k4a_image_t transformedBodyMaskImage;
    k4a_image_t bodyMaskImage;
    k4a_depth_mode_t depthCameraMode = K4A_DEPTH_MODE_OFF;

    AzureKinectCameraFrame* _cameraFrames[MAX_NUM_CACHED_BUFFERS];

    std::atomic_int _colorImageStride;
    int _depthImageStride;
    int _bodyMaskImageStride;

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    void RunBodyIndexLoop();

    void ReleaseBodyIndexMap(k4abt_frame_t bodyFrame, k4a_image_t bodyIndexMap);
    void SetBodyMaskBuffer(uint16_t* bodyMaskBuffer, uint8_t* bodyIndexBuffer, int bufferSize);
    void SetTransformedBodyMaskBuffer(uint16_t* transformedBodyMaskBuffer, int bufferSize);

    k4abt_tracker_t k4abtTracker;
    k4abt_tracker_configuration_t tracker_config = K4ABT_TRACKER_CONFIG_DEFAULT;
    std::shared_ptr<std::thread> _bodyIndexThread;
    std::atomic_int32_t _currentBodyMaskFrameIndex;
#endif

    std::shared_ptr<std::thread> _thread;
    std::atomic_bool _stopRequested;
    std::atomic_int32_t _currentFrameIndex;

    std::atomic_bool detectMarkers;
    float markerSize;
    cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName;

    std::mutex markerDetectorLock;
    std::shared_ptr<ArUcoMarkerDetector> markerDetector;
};
#endif