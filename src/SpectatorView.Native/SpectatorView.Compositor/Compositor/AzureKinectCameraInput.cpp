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
    , depthCameraMode(depthMode)
    , calibration()
    , k4aDevice(nullptr)
    , transformation(nullptr)
    , transformedDepthImage(nullptr)
    , transformedBodyMaskImage(nullptr)
    , bodyMaskImage(nullptr)
    , _stopRequested(false)
    , _currentFrameIndex(0)
    , detectMarkers(false)
    , markerSize(0.0f)
    , markerDictionaryName(cv::aruco::DICT_6X6_250)
    , markerDetector(new ArUcoMarkerDetector())
    , _colorImageStride(0)
    , _depthImageStride(0)
    , _bodyMaskImageStride(0)
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    , k4abtTracker(nullptr)
#endif
{
    if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &k4aDevice))
    {
        OutputDebugString(L"Failed to open AzureKinect device");
        goto FailedExit;
    }

    for (int i = 0; i < MAX_NUM_CACHED_BUFFERS; i++)
    {
        _cameraFrames[i] = new AzureKinectCameraFrame(captureDepth, captureBodyMask);
    }

    config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
    config.color_resolution = K4A_COLOR_RESOLUTION_1080P;
    config.depth_mode = depthCameraMode;
    config.camera_fps = K4A_FRAMES_PER_SECOND_30;

    if (K4A_RESULT_SUCCEEDED != k4a_device_start_cameras(k4aDevice, &config))
    {
        OutputDebugString(L"Failed to start AzureKinect camera");
        goto FailedExit;
    }

    if (captureDepth)
    {
        if (K4A_RESULT_SUCCEEDED != k4a_device_get_calibration(k4aDevice, config.depth_mode, config.color_resolution, &calibration))
        {
            OutputDebugString(L"Failed to get depth camera calibration");
            goto FailedExit;
        }

        transformation = k4a_transformation_create(&calibration);
        k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedDepthImage);
        _depthImageStride = k4a_image_get_stride_bytes(transformedDepthImage);

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
        if (captureBodyMask)
        {
            k4abt_tracker_configuration_t tracker_config = K4ABT_TRACKER_CONFIG_DEFAULT;
            if (K4A_RESULT_SUCCEEDED != k4abt_tracker_create(&calibration, tracker_config, &k4abtTracker))
            {
                OutputDebugString(L"Body tracker initialization failed!\n");
                goto FailedExit;
            }

            // Create new depth texture for body depth only
            k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.depth_camera_calibration.resolution_width, calibration.depth_camera_calibration.resolution_height, 2 * calibration.depth_camera_calibration.resolution_width, &bodyMaskImage);
            k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedBodyMaskImage);
            _bodyMaskImageStride = k4a_image_get_stride_bytes(transformedBodyMaskImage);
        }
#endif

    }

    _thread = std::make_shared<std::thread>(std::bind(&AzureKinectCameraInput::RunCaptureLoop, this));
    return;

FailedExit:
    if (k4aDevice != NULL)
    {
        k4a_device_close(k4aDevice);
    }
}

AzureKinectCameraInput::~AzureKinectCameraInput()
{
    _stopRequested = true;
    _thread->join();

    if (bodyMaskImage != nullptr)
    {
        k4a_image_release(bodyMaskImage);
        bodyMaskImage = nullptr;
    }

    if (transformedBodyMaskImage != nullptr)
    {
        k4a_image_release(transformedBodyMaskImage);
        transformedBodyMaskImage = nullptr;
    }

    if (transformedDepthImage != nullptr)
    {
        k4a_image_release(transformedDepthImage);
        transformedDepthImage = nullptr;
    }

    if (k4aDevice != nullptr)
    {
        k4a_device_stop_cameras(k4aDevice);
        k4a_device_close(k4aDevice);
        k4aDevice = nullptr;
    }

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    if (k4abtTracker != nullptr)
    {
        k4abt_tracker_shutdown(k4abtTracker);
        k4abt_tracker_destroy(k4abtTracker);
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
        if (!_cameraFrames[_currentFrameIndex % MAX_NUM_CACHED_BUFFERS]->TryBeginWriting())
        {
            Sleep(1);
            continue;
        }

        k4a_capture_t capture = nullptr;

        switch (k4a_device_get_capture(k4aDevice, &capture, K4A_WAIT_INFINITE))
        {
        case K4A_WAIT_RESULT_SUCCEEDED:
            break;
        case K4A_WAIT_RESULT_TIMEOUT:
            OutputDebugString(L"Timed out waiting for AzureKinect capture");
            return;
        case K4A_WAIT_RESULT_FAILED:
            OutputDebugString(L"Failed to capture from AzureKinect");
            return;
        }

        auto colorImage = k4a_capture_get_color_image(capture);
        if (colorImage != nullptr)
        {
            _colorImageStride = k4a_image_get_stride_bytes(colorImage);
            _cameraFrames[_currentFrameIndex % MAX_NUM_CACHED_BUFFERS]->StageImage(AzureKinectImageType::Color, colorImage);
            UpdateArUcoMarkers(colorImage);

            if (_captureDepth)
            {
                auto depthImage = k4a_capture_get_depth_image(capture);
                if (depthImage != nullptr)
                {
                    k4a_transformation_depth_image_to_color_camera(transformation, depthImage, transformedDepthImage);
                    _cameraFrames[_currentFrameIndex % MAX_NUM_CACHED_BUFFERS]->StageImage(AzureKinectImageType::Depth, transformedDepthImage);

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
                    if (_captureBodyMask)
                    {
                        auto height = k4a_image_get_height_pixels(depthImage);
                        auto width = k4a_image_get_width_pixels(depthImage);

                        uint16_t* bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(bodyMaskImage));

                        k4abt_frame_t bodyFrame;
                        k4a_image_t bodyMap;
                        uint8_t* bodyIndexBuffer;
                        GetBodyIndexMap(capture, &bodyFrame, &bodyMap, &bodyIndexBuffer);

                        if (bodyIndexBuffer != nullptr)
                        {
                            // Set body mask buffer to 0 where bodies are recognized  
                            SetBodyMaskBuffer(bodyMaskBuffer, bodyIndexBuffer, height * width);
                        }
                        ReleaseBodyIndexMap(bodyFrame, bodyMap);

                        k4a_result_t result = k4a_transformation_depth_image_to_color_camera(transformation, bodyMaskImage, transformedBodyMaskImage);

                        height = k4a_image_get_height_pixels(transformedBodyMaskImage);
                        width = k4a_image_get_width_pixels(transformedBodyMaskImage);

                        bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(transformedBodyMaskImage));

                        // Set transformed body mask buffer to 1 where bodies are not recognized  
                        SetTransformedBodyMaskBuffer(bodyMaskBuffer, height * width);

                        _cameraFrames[_currentFrameIndex % MAX_NUM_CACHED_BUFFERS]->StageImage(AzureKinectImageType::BodyMask, transformedBodyMaskImage);
                    }
#endif

                    k4a_image_release(depthImage);
                }
            }
            k4a_image_release(colorImage);
        }
        k4a_capture_release(capture);

        _cameraFrames[_currentFrameIndex % MAX_NUM_CACHED_BUFFERS]->EndWriting();
        _currentFrameIndex++;
    }
}

bool AzureKinectCameraInput::UpdateSRVs(int frameIndex, ID3D11Device* device, ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV)
{
    if (!_cameraFrames[frameIndex % MAX_NUM_CACHED_BUFFERS]->TryBeginReading())
    {
        return false;
    }

    _cameraFrames[frameIndex % MAX_NUM_CACHED_BUFFERS]->UpdateSRV(AzureKinectImageType::Color, device, colorSRV);
    _cameraFrames[frameIndex % MAX_NUM_CACHED_BUFFERS]->UpdateSRV(AzureKinectImageType::Depth, device, depthSRV);
    _cameraFrames[frameIndex % MAX_NUM_CACHED_BUFFERS]->UpdateSRV(AzureKinectImageType::BodyMask, device, bodySRV);
    _cameraFrames[frameIndex % MAX_NUM_CACHED_BUFFERS]->EndReading();
    return true;
}

void AzureKinectCameraInput::UpdateSRV(ID3D11Device* device, ID3D11ShaderResourceView* targetView, uint8_t* sourceBuffer, int stride)
{
    if (targetView != nullptr)
    {
        DirectXHelper::UpdateSRV(device, targetView, sourceBuffer, stride);
    }
}

void AzureKinectCameraInput::StageSRV(k4a_image_t image, uint8_t* targetBuffer, int targetBufferSize)
{
    auto stride = k4a_image_get_stride_bytes(image);
    auto buffer = k4a_image_get_buffer(image);
    auto height = k4a_image_get_height_pixels(image);

    memcpy_s(targetBuffer, targetBufferSize, buffer, height * stride);
}

void AzureKinectCameraInput::UpdateArUcoMarkers(k4a_image_t image)
{
    std::lock_guard<std::mutex> lockGuard(markerDetectorLock);

    if (detectMarkers)
    {
        auto height = k4a_image_get_height_pixels(image);
        auto width = k4a_image_get_width_pixels(image);
        auto buffer = k4a_image_get_buffer(image);

        const int radialDistortionCount = 6;
        const int tangentialDistortionCount = 2;

        float focalLength[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.fx, calibration.color_camera_calibration.intrinsics.parameters.param.fy };
        float principalPoint[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.cx, calibration.color_camera_calibration.intrinsics.parameters.param.cy };
        float radialDistortion[radialDistortionCount] = { calibration.color_camera_calibration.intrinsics.parameters.param.k1, calibration.color_camera_calibration.intrinsics.parameters.param.k2, calibration.color_camera_calibration.intrinsics.parameters.param.k3, calibration.color_camera_calibration.intrinsics.parameters.param.k4, calibration.color_camera_calibration.intrinsics.parameters.param.k5, calibration.color_camera_calibration.intrinsics.parameters.param.k6 };
        float tangentialDistortion[tangentialDistortionCount] = { calibration.color_camera_calibration.intrinsics.parameters.param.p1, calibration.color_camera_calibration.intrinsics.parameters.param.p2 };
        markerDetector->DetectMarkers(buffer, width, height, focalLength, principalPoint, radialDistortion, radialDistortionCount, tangentialDistortion, tangentialDistortionCount, markerSize, markerDictionaryName);
    }
}

void AzureKinectCameraInput::StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize)
{
    std::lock_guard<std::mutex> lockGuard(markerDetectorLock);

    this->markerDetector->Reset();
    this->markerDictionaryName = markerDictionaryName;
    this->markerSize = markerSize;
    this->detectMarkers = true;
}

void AzureKinectCameraInput::StopArUcoMarkerDetector()
{
    std::lock_guard<std::mutex> lockGuard(markerDetectorLock);

    this->detectMarkers = false;
}

void AzureKinectCameraInput::GetLatestArUcoMarkers(int size, Marker* markers)
{
    std::lock_guard<std::mutex> lockGuard(markerDetectorLock);

    if (this->detectMarkers)
    {
        int localSize = markerDetector->GetDetectedMarkersCount();
        std::vector<int> markerIds;
        markerIds.resize(localSize);
        markerDetector->GetDetectedMarkerIds(markerIds.data(), localSize);

        for (int i = 0; i < localSize && i < size; i++)
        {
            markers[i].id = markerIds[i];
            markerDetector->GetDetectedMarkerPose(markerIds[i], &markers[i].position, &markers[i].rotation);
        }
    }
}

void AzureKinectCameraInput::GetCameraCalibrationInformation(CameraIntrinsics* calibration)
{
    calibration->focalLength = { this->calibration.color_camera_calibration.intrinsics.parameters.param.fx, this->calibration.color_camera_calibration.intrinsics.parameters.param.fy };
    calibration->principalPoint = { this->calibration.color_camera_calibration.intrinsics.parameters.param.cx, this->calibration.color_camera_calibration.intrinsics.parameters.param.cy };
    calibration->imageWidth = this->calibration.color_camera_calibration.resolution_width;
    calibration->imageHeight = this->calibration.color_camera_calibration.resolution_height;
}

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
void AzureKinectCameraInput::GetBodyIndexMap(k4a_capture_t capture, k4abt_frame_t* bodyFrame, k4a_image_t* bodyIndexMap, uint8_t** bodyIndexBuffer)
{
    k4a_wait_result_t queue_capture_result = k4abt_tracker_enqueue_capture(k4abtTracker, capture, K4A_WAIT_INFINITE);

    if (queue_capture_result == K4A_WAIT_RESULT_FAILED)
    {
        printf("Error! Adding capture to tracker process queue failed!\n");
        *bodyFrame = nullptr;
        *bodyIndexMap = nullptr;
        *bodyIndexBuffer = nullptr;
    }
    else
    {
        k4a_wait_result_t pop_frame_result = k4abt_tracker_pop_result(k4abtTracker, bodyFrame, K4A_WAIT_INFINITE);
        if (pop_frame_result == K4A_WAIT_RESULT_SUCCEEDED)
        {
            *bodyIndexMap = k4abt_frame_get_body_index_map(*bodyFrame);
            *bodyIndexBuffer = k4a_image_get_buffer(*bodyIndexMap);
        }
        else
        {
            *bodyIndexMap = nullptr;
            *bodyIndexBuffer = nullptr;
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