// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "AzureKinectCameraInput.h"
#include "ArUcoMarkerDetector.h"
#if defined(INCLUDE_AZUREKINECT)
#include <opencv2\aruco.hpp>
#include <k4a/k4a.h>
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
#include <k4abt.h>
#endif

AzureKinectCameraInput::AzureKinectCameraInput(k4a_depth_mode_t depthMode, bool captureDepth, bool captureBodyMask)
    : _captureDepth(captureDepth)
    , _captureBodyMask(captureBodyMask)
    , _depthCameraMode(depthMode)
    , _calibration()
    , _k4aDevice(nullptr)
    , _transformation(nullptr)
    , _transformedDepthImage(nullptr)
    , _transformedBodyMaskImage(nullptr)
    , _bodyMaskImage(nullptr)
    , _stopRequested(false)
    , _currentFrameIndex(0)
    , _detectMarkers(false)
    , _markerSize(0.0f)
    , _markerDictionaryName(cv::aruco::DICT_6X6_250)
    , _markerDetector(new ArUcoMarkerDetector())
    , _colorImageStride(0)
    , _depthImageStride(0)
    , _bodyMaskImageStride(0)
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    , _currentBodyMaskFrameIndex(0)
    , _k4abtTracker(nullptr)
#endif
{
    if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &_k4aDevice))
    {
        OutputDebugString(L"Failed to open AzureKinect device");
        goto FailedExit;
    }

    for (int i = 0; i < MAX_NUM_CACHED_BUFFERS; i++)
    {
        _cameraFrames[i] = new AzureKinectCameraFrame(captureDepth, captureBodyMask);
    }

    _config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
    _config.color_resolution = K4A_COLOR_RESOLUTION_1080P;
    _config.depth_mode = _depthCameraMode;
    _config.camera_fps = K4A_FRAMES_PER_SECOND_30;

    if (K4A_RESULT_SUCCEEDED != k4a_device_start_cameras(_k4aDevice, &_config))
    {
        OutputDebugString(L"Failed to start AzureKinect camera");
        goto FailedExit;
    }

    if (K4A_RESULT_SUCCEEDED != k4a_device_get_calibration(_k4aDevice, _config.depth_mode, _config.color_resolution, &_calibration))
    {
        OutputDebugString(L"Failed to get camera calibration");
        goto FailedExit;
    }

    if (captureDepth)
    {
        _transformation = k4a_transformation_create(&_calibration);
        k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, _calibration.color_camera_calibration.resolution_width, _calibration.color_camera_calibration.resolution_height, 2 * _calibration.color_camera_calibration.resolution_width, &_transformedDepthImage);
        _depthImageStride = k4a_image_get_stride_bytes(_transformedDepthImage);

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
        if (captureBodyMask)
        {
            k4abt_tracker_configuration_t tracker_config = K4ABT_TRACKER_CONFIG_DEFAULT;
            if (K4A_RESULT_SUCCEEDED != k4abt_tracker_create(&_calibration, tracker_config, &_k4abtTracker))
            {
                OutputDebugString(L"Body tracker initialization failed!\n");
                goto FailedExit;
            }

            // Create new depth texture for body depth only
            k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, _calibration.depth_camera_calibration.resolution_width, _calibration.depth_camera_calibration.resolution_height, 2 * _calibration.depth_camera_calibration.resolution_width, &_bodyMaskImage);
            k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, _calibration.color_camera_calibration.resolution_width, _calibration.color_camera_calibration.resolution_height, 2 * _calibration.color_camera_calibration.resolution_width, &_transformedBodyMaskImage);
            _bodyMaskImageStride = k4a_image_get_stride_bytes(_transformedBodyMaskImage);
        }
#endif

    }

    _thread = std::make_shared<std::thread>(std::bind(&AzureKinectCameraInput::RunCaptureLoop, this));

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    if (captureBodyMask)
    {
        _bodyIndexThread = std::make_shared<std::thread>(std::bind(&AzureKinectCameraInput::RunBodyIndexLoop, this));
    }
#endif

    return;

FailedExit:
    if (_k4aDevice != NULL)
    {
        k4a_device_close(_k4aDevice);
    }
}

AzureKinectCameraInput::~AzureKinectCameraInput()
{
    _stopRequested = true;
    _thread->join();

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    if (_bodyIndexThread != nullptr)
    {
        _bodyIndexThread->join();
    }
#endif

    if (_bodyMaskImage != nullptr)
    {
        k4a_image_release(_bodyMaskImage);
        _bodyMaskImage = nullptr;
    }

    if (_transformedBodyMaskImage != nullptr)
    {
        k4a_image_release(_transformedBodyMaskImage);
        _transformedBodyMaskImage = nullptr;
    }

    if (_transformedDepthImage != nullptr)
    {
        k4a_image_release(_transformedDepthImage);
        _transformedDepthImage = nullptr;
    }

    if (_k4aDevice != nullptr)
    {
        k4a_device_stop_cameras(_k4aDevice);
        k4a_device_close(_k4aDevice);
        _k4aDevice = nullptr;
    }

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    if (_k4abtTracker != nullptr)
    {
        k4abt_tracker_shutdown(_k4abtTracker);
        k4abt_tracker_destroy(_k4abtTracker);
    }
#endif

    for (int i = 0; i < MAX_NUM_CACHED_BUFFERS; i++)
    {
        delete _cameraFrames[i];
    }
}

void AzureKinectCameraInput::RunCaptureLoop()
{
    while (!_stopRequested)
    {
        int frameIndex = _currentFrameIndex % MAX_NUM_CACHED_BUFFERS;
        if (!_cameraFrames[frameIndex]->TryBeginWritingColorAndDepth())
        {
            // If the next frame in the buffer is still pending write or is being read from, then we've
            // exceeded the capacity of the buffer and either the reader is blocking too long, or
            // the body index processing thread has fallen behind.
            OutputDebugString(L"Warning: frame buffer is completely full, and we can't begin writing to the next frame");
            continue;
        }

        k4a_capture_t capture = nullptr;

        switch (k4a_device_get_capture(_k4aDevice, &capture, K4A_WAIT_INFINITE))
        {
        case K4A_WAIT_RESULT_TIMEOUT:
            OutputDebugString(L"Error: Timed out waiting for AzureKinect capture");
            return;
        case K4A_WAIT_RESULT_FAILED:
            OutputDebugString(L"Error: Failed to capture from AzureKinect");
            return;
        }

        auto colorImage = k4a_capture_get_color_image(capture);
        if (colorImage != nullptr)
        {
            _colorImageStride = k4a_image_get_stride_bytes(colorImage);
            _cameraFrames[frameIndex]->StageImage(AzureKinectImageType::Color, colorImage);
            UpdateArUcoMarkers(colorImage);

            if (_captureDepth)
            {
                auto depthImage = k4a_capture_get_depth_image(capture);
                if (depthImage != nullptr)
                {
                    k4a_transformation_depth_image_to_color_camera(_transformation, depthImage, _transformedDepthImage);
                    _cameraFrames[frameIndex]->StageImage(AzureKinectImageType::Depth, _transformedDepthImage);

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
                    if (_captureBodyMask)
                    {
                        // Move to the state of writing the body mask before enqueuing the capture.
                        // This will put the frame in a state where it's waiting for the body mask
                        // before entering the Staged state.
                        _cameraFrames[frameIndex]->BeginWritingBodyMask();
                        k4a_wait_result_t queue_capture_result = k4abt_tracker_enqueue_capture(_k4abtTracker, capture, K4A_WAIT_INFINITE);

                        if (queue_capture_result == K4A_WAIT_RESULT_FAILED)
                        {
                            printf("Error: Adding capture to tracker process queue failed!\n");
                            _cameraFrames[frameIndex]->EndWritingBodyMask();
                        }
                    }
#endif

                    k4a_image_release(depthImage);
                }
            }
            k4a_image_release(colorImage);
        }
        k4a_capture_release(capture);

        // End the writing state. If the state machine already transitioned
        // to WritingBodyMask, this will have no effect. If the state machine
        // is still in the WritingColorAndDepth state, it will move to the 
        // Staged state to mark the frame as ready for reading.
        _cameraFrames[frameIndex]->EndWritingColorAndDepth();
        _currentFrameIndex++;
    }
}

bool AzureKinectCameraInput::UpdateSRVs(int frameIndex, ID3D11Device* device, ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV)
{
    int cameraFrameIndex = frameIndex % MAX_NUM_CACHED_BUFFERS;
    if (!_cameraFrames[cameraFrameIndex]->TryBeginReading())
    {
        // If the target frame is not ready to be read from yet because it's
        // still being written to, or because we've already consumed that frame,
        // then there's no need to update the target shader resource views again.
        return false;
    }

    _cameraFrames[cameraFrameIndex]->UpdateSRV(AzureKinectImageType::Color, device, colorSRV);
    _cameraFrames[cameraFrameIndex]->UpdateSRV(AzureKinectImageType::Depth, device, depthSRV);
    _cameraFrames[cameraFrameIndex]->UpdateSRV(AzureKinectImageType::BodyMask, device, bodySRV);
    _cameraFrames[cameraFrameIndex]->EndReading();
    return true;
}

void AzureKinectCameraInput::UpdateArUcoMarkers(k4a_image_t image)
{
    std::lock_guard<std::mutex> lockGuard(_markerDetectorLock);

    if (_detectMarkers)
    {
        auto height = k4a_image_get_height_pixels(image);
        auto width = k4a_image_get_width_pixels(image);
        auto buffer = k4a_image_get_buffer(image);

        const int radialDistortionCount = 6;
        const int tangentialDistortionCount = 2;

        float focalLength[2] = { _calibration.color_camera_calibration.intrinsics.parameters.param.fx, _calibration.color_camera_calibration.intrinsics.parameters.param.fy };
        float principalPoint[2] = { _calibration.color_camera_calibration.intrinsics.parameters.param.cx, _calibration.color_camera_calibration.intrinsics.parameters.param.cy };
        float radialDistortion[radialDistortionCount] = { _calibration.color_camera_calibration.intrinsics.parameters.param.k1, _calibration.color_camera_calibration.intrinsics.parameters.param.k2, _calibration.color_camera_calibration.intrinsics.parameters.param.k3, _calibration.color_camera_calibration.intrinsics.parameters.param.k4, _calibration.color_camera_calibration.intrinsics.parameters.param.k5, _calibration.color_camera_calibration.intrinsics.parameters.param.k6 };
        float tangentialDistortion[tangentialDistortionCount] = { _calibration.color_camera_calibration.intrinsics.parameters.param.p1, _calibration.color_camera_calibration.intrinsics.parameters.param.p2 };
        _markerDetector->DetectMarkers(buffer, width, height, focalLength, principalPoint, radialDistortion, radialDistortionCount, tangentialDistortion, tangentialDistortionCount, _markerSize, _markerDictionaryName);
    }
}

void AzureKinectCameraInput::StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize)
{
    std::lock_guard<std::mutex> lockGuard(_markerDetectorLock);

    this->_markerDetector->Reset();
    this->_markerDictionaryName = markerDictionaryName;
    this->_markerSize = markerSize;
    this->_detectMarkers = true;
}

void AzureKinectCameraInput::StopArUcoMarkerDetector()
{
    std::lock_guard<std::mutex> lockGuard(_markerDetectorLock);

    this->_detectMarkers = false;
}

void AzureKinectCameraInput::GetLatestArUcoMarkers(int size, Marker* markers)
{
    std::lock_guard<std::mutex> lockGuard(_markerDetectorLock);

    if (this->_detectMarkers)
    {
        int localSize = _markerDetector->GetDetectedMarkersCount();
        std::vector<int> markerIds;
        markerIds.resize(localSize);
        _markerDetector->GetDetectedMarkerIds(markerIds.data(), localSize);

        for (int i = 0; i < localSize && i < size; i++)
        {
            markers[i].id = markerIds[i];
            _markerDetector->GetDetectedMarkerPose(markerIds[i], &markers[i].position, &markers[i].rotation);
        }
    }
}

void AzureKinectCameraInput::GetCameraCalibrationInformation(CameraIntrinsics* calibration)
{
    calibration->focalLength = { this->_calibration.color_camera_calibration.intrinsics.parameters.param.fx, this->_calibration.color_camera_calibration.intrinsics.parameters.param.fy };
    calibration->principalPoint = { this->_calibration.color_camera_calibration.intrinsics.parameters.param.cx, this->_calibration.color_camera_calibration.intrinsics.parameters.param.cy };
    calibration->imageWidth = this->_calibration.color_camera_calibration.resolution_width;
    calibration->imageHeight = this->_calibration.color_camera_calibration.resolution_height;
}

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
void AzureKinectCameraInput::RunBodyIndexLoop()
{
    while (!_stopRequested)
    {
        k4abt_frame_t bodyFrame;
        k4a_wait_result_t pop_frame_result = k4abt_tracker_pop_result(_k4abtTracker, &bodyFrame, BODY_INDEX_WAIT_TIME_MILLISECONDS);
        if (pop_frame_result == K4A_WAIT_RESULT_SUCCEEDED)
        {
            uint16_t* bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(_bodyMaskImage));
            auto height = k4a_image_get_height_pixels(_bodyMaskImage);
            auto width = k4a_image_get_width_pixels(_bodyMaskImage);

            k4a_image_t bodyIndexMap = k4abt_frame_get_body_index_map(bodyFrame);
            uint8_t* bodyIndexBuffer = k4a_image_get_buffer(bodyIndexMap);

            if (bodyIndexBuffer != nullptr)
            {
                // Set body mask buffer to 0 where bodies are recognized  
                SetBodyMaskBuffer(bodyMaskBuffer, bodyIndexBuffer, height * width);
            }
            ReleaseBodyIndexMap(bodyFrame, bodyIndexMap);

            k4a_result_t result = k4a_transformation_depth_image_to_color_camera(_transformation, _bodyMaskImage, _transformedBodyMaskImage);

            height = k4a_image_get_height_pixels(_transformedBodyMaskImage);
            width = k4a_image_get_width_pixels(_transformedBodyMaskImage);

            bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(_transformedBodyMaskImage));

            // Set transformed body mask buffer to 1 where bodies are not recognized  
            SetTransformedBodyMaskBuffer(bodyMaskBuffer, height * width);

            // Stage the body mask image, and then end the WritingBodyMask state.
            // This will transition the frame to the Staged state.
            int frameIndex = _currentBodyMaskFrameIndex % MAX_NUM_CACHED_BUFFERS;
            _cameraFrames[frameIndex]->StageImage(AzureKinectImageType::BodyMask, _transformedBodyMaskImage);
            _cameraFrames[frameIndex]->EndWritingBodyMask();

            _currentBodyMaskFrameIndex++;
        }
    }
}

void AzureKinectCameraInput::ReleaseBodyIndexMap(k4abt_frame_t bodyFrame, k4a_image_t bodyIndexMap)
{
    if (bodyIndexMap != nullptr)
    {
        k4a_image_release(bodyIndexMap);
    }

    if (bodyFrame != nullptr)
    {
        k4abt_frame_release(bodyFrame);
    }
}

void AzureKinectCameraInput::SetBodyMaskBuffer(uint16_t* bodyMaskBuffer, uint8_t* bodyIndexBuffer, int bufferSize)
{
    for (int i = 0; i < bufferSize; i++)
    {
        if (bodyIndexBuffer[i] != K4ABT_BODY_INDEX_MAP_BACKGROUND)
        {
            // Using 1000 to ensure value is not truncated to 0 by depth to color transformation
            bodyMaskBuffer[i] = 1000;
        }
        else
        {
            bodyMaskBuffer[i] = 0;
        }
    }
}

void AzureKinectCameraInput::SetTransformedBodyMaskBuffer(uint16_t* transformedBodyMaskBuffer, int bufferSize)
{
    for (int i = 0; i < bufferSize; i++)
    {
        if (transformedBodyMaskBuffer[i] > 0)
        {
            transformedBodyMaskBuffer[i] = 1;
        }
    }
}
#endif
#endif