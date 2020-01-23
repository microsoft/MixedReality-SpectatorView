// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "stdafx.h"

#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectFrameProvider.h"
#include <k4a/k4a.h>
#include <k4abt.h>

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

    if (K4A_RESULT_SUCCEEDED != k4a_device_get_calibration(k4aDevice, config.depth_mode, config.color_resolution, &calibration))
    {
        OutputDebugString(L"Failed to get depth camera calibration");
        goto FailedExit;
    }

    k4abt_tracker_configuration_t tracker_config = K4ABT_TRACKER_CONFIG_DEFAULT;
    if (K4A_RESULT_SUCCEEDED != k4abt_tracker_create(&calibration, tracker_config, &k4abtTracker))
    {
        OutputDebugString(L"Body tracker initialization failed!\n");
        goto FailedExit;
    }

    transformation = k4a_transformation_create(&calibration);

    k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedDepthImage);
    k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, calibration.color_camera_calibration.resolution_width, calibration.color_camera_calibration.resolution_height, 2 * calibration.color_camera_calibration.resolution_width, &transformedBodyDepthImage);
    
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
    
    UpdateSRV(colorImage, _colorSRV);

    k4a_transformation_depth_image_to_color_camera(transformation, depthImage, transformedDepthImage);

    UpdateSRV(transformedDepthImage, _depthSRV);

    auto height = k4a_image_get_height_pixels(depthImage);
    auto width = k4a_image_get_width_pixels(depthImage);
    auto stride = k4a_image_get_stride_bytes(depthImage);

    // Create new depth texture for body depth only
    k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, width, height, stride, &bodyDepthImage);

    uint16_t* bodyDepthBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(bodyDepthImage));
    uint16_t* depthBuffer = reinterpret_cast<uint16_t*>(k4a_image_get_buffer(depthImage));
    uint8_t* bodyIndexBuffer = GetBodyIndexBuffer(capture);

    // Set body depth buffer to depth values where a body is identified 
    SetBodyDepthBuffer(bodyDepthBuffer, depthBuffer, bodyIndexBuffer, height * width);

    k4a_result_t result = k4a_transformation_depth_image_to_color_camera(transformation, bodyDepthImage, transformedBodyDepthImage);
    
    UpdateSRV(transformedBodyDepthImage, _bodySRV);

    k4a_image_release(depthImage);
    k4a_image_release(colorImage);
    k4a_capture_release(capture);
}

void AzureKinectFrameProvider::UpdateSRV(k4a_image_t image, ID3D11ShaderResourceView* _srv)
{
    auto stride = k4a_image_get_stride_bytes(image);
    auto buffer = k4a_image_get_buffer(image);

    DirectXHelper::UpdateSRV(d3d11Device, _srv, buffer, stride);
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

void AzureKinectFrameProvider::SetBodyDepthBuffer(uint16_t* bodyDepthBuffer, uint16_t* depthBuffer, uint8_t* bodyIndexBuffer, int bufferSize)
{
    int bufferIndex = 0;

    //Copy depth values only if bodyID is not K4ABT_BODY_INDEX_MAP_BACKGROUND
    while (bufferIndex < bufferSize)
    {
        if (bodyIndexBuffer[bufferIndex] != K4ABT_BODY_INDEX_MAP_BACKGROUND)
        {
            bodyDepthBuffer[bufferIndex] = depthBuffer[bufferIndex];
        }
        else
        {
            bodyDepthBuffer[bufferIndex] = 0;
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
#endif