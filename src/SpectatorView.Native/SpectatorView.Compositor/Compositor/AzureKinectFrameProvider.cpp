// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "stdafx.h"

#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectFrameProvider.h"
#include <k4a/k4a.h>

AzureKinectFrameProvider::AzureKinectFrameProvider()
    : detectMarkers(false)
    , markerSize(0.0f)
    , _captureFrameIndex(0)
    , markerDetector(new ArUcoMarkerDetector())
    , _colorSRV(nullptr)
    , _depthSRV(nullptr)
    , calibration()
    , d3d11Device(nullptr)
    , k4aDevice(nullptr)
    , lock()
    , transformation(nullptr)
    , transformedDepthImage(nullptr)
{}

HRESULT AzureKinectFrameProvider::Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11Texture2D* outputTexture)
{
    InitializeCriticalSection(&lock);

    _captureFrameIndex = 0;
    _depthSRV = depthSRV;
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

    k4a_device_get_calibration(k4aDevice, config.depth_mode, config.color_resolution, &calibration);
    transformation = k4a_transformation_create(&calibration);

    k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedDepthImage);
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
    auto depthImage = k4a_capture_get_depth_image(capture);
    if (colorImage == nullptr || depthImage == nullptr)
    {
        return;
    }

    auto height = k4a_image_get_height_pixels(colorImage);
    auto width = k4a_image_get_width_pixels(colorImage);
    auto stride = k4a_image_get_stride_bytes(colorImage);
    auto buffer = k4a_image_get_buffer(colorImage);

    DirectXHelper::UpdateSRV(d3d11Device, _colorSRV, buffer, stride);

    if (detectMarkers)
    {
        float focalLength[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.fx, calibration.color_camera_calibration.intrinsics.parameters.param.fy };
        float principalPoint[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.cx, calibration.color_camera_calibration.intrinsics.parameters.param.cy };
        float radialDistortion[3] = { calibration.color_camera_calibration.intrinsics.parameters.param.k1, calibration.color_camera_calibration.intrinsics.parameters.param.k2, calibration.color_camera_calibration.intrinsics.parameters.param.k3 };
        float tangentialDistortion[2] = { calibration.color_camera_calibration.intrinsics.parameters.param.p1, calibration.color_camera_calibration.intrinsics.parameters.param.p2 };
        markerDetector->DetectMarkers(buffer, width, height, focalLength, principalPoint, radialDistortion, tangentialDistortion, markerSize, cv::aruco::DICT_6X6_250);
    }

    k4a_transformation_depth_image_to_color_camera(transformation, depthImage, transformedDepthImage);

    stride = k4a_image_get_stride_bytes(transformedDepthImage);
    buffer = k4a_image_get_buffer(transformedDepthImage);
    DirectXHelper::UpdateSRV(d3d11Device, _depthSRV, buffer, stride);

    k4a_image_release(depthImage);
    k4a_image_release(colorImage);
    k4a_capture_release(capture);
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
    if (transformedDepthImage != nullptr)
    {
        k4a_image_release(transformedDepthImage);
        transformedDepthImage = nullptr;
    }

    if (k4aDevice != nullptr)
    {
        k4a_device_close(k4aDevice);
        k4aDevice = nullptr;
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