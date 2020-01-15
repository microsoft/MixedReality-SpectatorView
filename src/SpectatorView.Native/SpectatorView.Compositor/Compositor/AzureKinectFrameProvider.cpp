// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "stdafx.h"

#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectFrameProvider.h"
#include <k4a/k4a.h>

HRESULT AzureKinectFrameProvider::Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11Texture2D* outputTexture)
{
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
        break;
    case K4A_WAIT_RESULT_TIMEOUT:
        OutputDebugString(L"Timed out waiting for AzureKinect capture");
        return;
    case K4A_WAIT_RESULT_FAILED:
        OutputDebugString(L"Failed to capture from AzureKinect");
        return;
    }

    auto colorImage = k4a_capture_get_color_image(capture);

    auto height = k4a_image_get_height_pixels(colorImage);
    auto width = k4a_image_get_width_pixels(colorImage);
    auto stride = k4a_image_get_stride_bytes(colorImage);
    auto bpp = stride / width;
    auto buffer = k4a_image_get_buffer(colorImage);

    DirectXHelper::UpdateSRV(d3d11Device, _colorSRV, buffer, stride);

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
    if (k4aDevice != nullptr)
    {
        k4a_device_close(k4aDevice);
        k4aDevice = nullptr;
    }
}

bool AzureKinectFrameProvider::OutputYUV()
{
    return false;
}
#endif