// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "stdafx.h"

#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectFrameProvider.h"
#include <k4a/k4a.h>
#include <k4abt.h>

AzureKinectFrameProvider::AzureKinectFrameProvider()
    : detectMarkers(false)
    , markerSize(0.0f)
    , _captureFrameIndex(0)
    , markerDetector(new ArUcoMarkerDetector())
    , _colorSRV(nullptr)
    , _depthSRV(nullptr)
    , _bodySRV(nullptr)
    , calibration()
    , d3d11Device(nullptr)
    , k4aDevice(nullptr)
    , lock()
    , transformation(nullptr)
    , transformedDepthImage(nullptr)
    , transformedBodyMaskImage(nullptr)
    , bodyMaskImage(nullptr)
{}

HRESULT AzureKinectFrameProvider::Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture)
{
    InitializeCriticalSection(&lock);

    _captureFrameIndex = 0;
    _depthSRV = depthSRV;
    _bodySRV = bodySRV;
    _colorSRV = colorSRV;
    _colorSRV->GetDevice(&d3d11Device);

    if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &k4aDevice))
    {
        OutputDebugString(L"Failed to open AzureKinect device");
        goto FailedExit;
    }

    config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
    config.color_resolution = K4A_COLOR_RESOLUTION_1080P;
    config.depth_mode = K4A_DEPTH_MODE_WFOV_2X2BINNED;
    config.camera_fps = K4A_FRAMES_PER_SECOND_30;

    if (K4A_RESULT_SUCCEEDED != k4a_device_start_cameras(k4aDevice, &config))
    {
        OutputDebugString(L"Failed to start AzureKinect camera");
        goto FailedExit;
    }

    if (depthSRV != nullptr)
    {
        if (K4A_RESULT_SUCCEEDED != k4a_device_get_calibration(k4aDevice, config.depth_mode, config.color_resolution, &calibration))
        {
            OutputDebugString(L"Failed to get depth camera calibration");
            goto FailedExit;
        }

        transformation = k4a_transformation_create(&calibration);
        k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedDepthImage);

        if(bodySRV != nullptr)
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
        }
    }

    return S_OK;

FailedExit:
    if (k4aDevice != NULL)
    {
        k4a_device_close(k4aDevice);
    }
    return E_FAIL;
}

LONGLONG AzureKinectFrameProvider::GetTimestamp(int frame)
{
    return LONGLONG();
}

LONGLONG AzureKinectFrameProvider::GetDurationHNS()
{
    return (LONGLONG)((1.0f / 30.0f) * QPC_MULTIPLIER);
}

void AzureKinectFrameProvider::Update(int compositeFrameIndex)
{
    k4a_capture_t capture = nullptr;

    switch (k4a_device_get_capture(k4aDevice, &capture, 0))
    {
    case K4A_WAIT_RESULT_SUCCEEDED:
        _captureFrameIndex++;
        break;
    case K4A_WAIT_RESULT_TIMEOUT:
        OutputDebugString(L"Timed out waiting for AzureKinect capture");
        return;
    case K4A_WAIT_RESULT_FAILED:
        OutputDebugString(L"Failed to capture from AzureKinect");
        return;
    }
   
    auto colorImage = k4a_capture_get_color_image(capture);
    if (colorImage == nullptr)
    {
        return;
    }
    
    UpdateSRV(colorImage, _colorSRV);
    UpdateArUcoMarkers(colorImage);

    if (_depthSRV != nullptr)
    {
        auto depthImage = k4a_capture_get_depth_image(capture);
        if (depthImage == nullptr)
        {
            return;
        }

        if (_bodySRV != nullptr)
        {
            auto height = k4a_image_get_height_pixels(depthImage);
            auto width = k4a_image_get_width_pixels(depthImage);

            uint16_t* bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(bodyMaskImage));
            uint8_t* bodyIndexBuffer = GetBodyIndexBuffer(capture);

            // Set body mask buffer to 0 where bodies are recognized  
            SetBodyMaskBuffer(bodyMaskBuffer, bodyIndexBuffer, height * width);

            k4a_result_t result = k4a_transformation_depth_image_to_color_camera(transformation, bodyMaskImage, transformedBodyMaskImage);

            height = k4a_image_get_height_pixels(transformedBodyMaskImage);
            width = k4a_image_get_width_pixels(transformedBodyMaskImage);

            bodyMaskBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(transformedBodyMaskImage));

            // Set transformed body mask buffer to 1 where bodies are not recognized  
            SetTransformedBodyMaskBuffer(bodyMaskBuffer, height * width);

            UpdateSRV(transformedBodyMaskImage, _bodySRV);
            }

        k4a_image_release(depthImage);
        }
    }
    k4a_image_release(colorImage);
    k4a_capture_release(capture);
}

void AzureKinectFrameProvider::UpdateSRV(k4a_image_t image, ID3D11ShaderResourceView* _srv)
{
    auto stride = k4a_image_get_stride_bytes(image);
    auto buffer = k4a_image_get_buffer(image);

    DirectXHelper::UpdateSRV(d3d11Device, _srv, buffer, stride);
}

void AzureKinectFrameProvider::UpdateArUcoMarkers(k4a_image_t image)
{
    if (detectMarkers)
    {
        auto height = k4a_image_get_height_pixels(image);
        auto width = k4a_image_get_width_pixels(image);
        auto buffer = k4a_image_get_buffer(image);

        float focalLength[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.fx, calibration.color_camera_calibration.intrinsics.parameters.param.fy };
        float principalPoint[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.cx, calibration.color_camera_calibration.intrinsics.parameters.param.cy };
        float radialDistortion[3] = { calibration.color_camera_calibration.intrinsics.parameters.param.k1, calibration.color_camera_calibration.intrinsics.parameters.param.k2, calibration.color_camera_calibration.intrinsics.parameters.param.k3 };
        float tangentialDistortion[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.p1, calibration.color_camera_calibration.intrinsics.parameters.param.p2 };
        markerDetector->DetectMarkers(buffer, width, height, focalLength, principalPoint, radialDistortion, tangentialDistortion, markerSize, cv::aruco::DICT_6X6_250);
    }
}

uint8_t* AzureKinectFrameProvider::GetBodyIndexBuffer(k4a_capture_t capture)
{
    uint8_t* bodyIndexBuffer;

    k4a_wait_result_t queue_capture_result = k4abt_tracker_enqueue_capture(k4abtTracker, capture, K4A_WAIT_INFINITE);

    if (queue_capture_result == K4A_WAIT_RESULT_FAILED)
    {
        printf("Error! Adding capture to tracker process queue failed!\n");
        return NULL;
    }

    k4abt_frame_t body_frame = NULL;
    k4a_wait_result_t pop_frame_result = k4abt_tracker_pop_result(k4abtTracker, &body_frame, K4A_WAIT_INFINITE);
    if (pop_frame_result == K4A_WAIT_RESULT_SUCCEEDED)
    {
        auto bodyIndexMap = k4abt_frame_get_body_index_map(body_frame);
        bodyIndexBuffer = k4a_image_get_buffer(bodyIndexMap);
        k4a_image_release(bodyIndexMap);
    }
   
    k4abt_frame_release(body_frame);
    return bodyIndexBuffer;
}

void AzureKinectFrameProvider::SetBodyMaskBuffer(uint16_t* bodyMaskBuffer, uint8_t* bodyIndexBuffer, int bufferSize)
{
    int bufferIndex = 0;

    while (bufferIndex < bufferSize)
    {
        if (bodyIndexBuffer[bufferIndex] != K4ABT_BODY_INDEX_MAP_BACKGROUND)
        {
            bodyMaskBuffer[bufferIndex] = 0;
        }
        else
        {
            // Using max short value to ensure value is not truncated to 0 by depth to color transformation
            bodyMaskBuffer[bufferIndex] = USHRT_MAX;
        }

        bufferIndex++;
    }
}

void AzureKinectFrameProvider::SetTransformedBodyMaskBuffer(uint16_t* transformedBodyMaskBuffer, int bufferSize)
{
    int bufferIndex = 0;

    while (bufferIndex < bufferSize)
    {
        if (transformedBodyMaskBuffer[bufferIndex] > 0)
        {
            bodyMaskBuffer[bufferIndex] = 1;
        }

        bufferIndex++;
    }
}

IFrameProvider::ProviderType AzureKinectFrameProvider::GetProviderType()
{
    return IFrameProvider::ProviderType::AzureKinect;
}

bool AzureKinectFrameProvider::IsEnabled()
{
    return k4aDevice != nullptr;
}

bool AzureKinectFrameProvider::SupportsOutput()
{
    return true;
}

void AzureKinectFrameProvider::Dispose()
{
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

    if (k4abtTracker != nullptr)
    {
        k4abt_tracker_shutdown(k4abtTracker);
        k4abt_tracker_destroy(k4abtTracker);
    }

    DeleteCriticalSection(&lock);
}

bool AzureKinectFrameProvider::OutputYUV()
{
    return false;
}

void AzureKinectFrameProvider::GetCameraCalibrationInformation(CameraIntrinsics* calibration)
{
    calibration->focalLength = { this->calibration.color_camera_calibration.intrinsics.parameters.param.fx, this->calibration.color_camera_calibration.intrinsics.parameters.param.fy };
    calibration->principalPoint = { this->calibration.color_camera_calibration.intrinsics.parameters.param.cx, this->calibration.color_camera_calibration.intrinsics.parameters.param.cy };
    calibration->imageWidth = this->calibration.color_camera_calibration.resolution_width;
    calibration->imageHeight = this->calibration.color_camera_calibration.resolution_height;
}

void AzureKinectFrameProvider::StartArUcoMarkerDetector(float markerSize)
{
    this->markerDetector->Reset();
    this->markerSize = markerSize;
    this->detectMarkers = true;
}

void AzureKinectFrameProvider::StopArUcoMarkerDetector()
{
    this->detectMarkers = false;
}

void AzureKinectFrameProvider::GetLatestArUcoMarkers(int size, Marker* markers)
{
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
#endif