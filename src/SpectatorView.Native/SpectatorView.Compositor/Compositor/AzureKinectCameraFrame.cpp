// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "AzureKinectCameraFrame.h"

AzureKinectCameraFrame::AzureKinectCameraFrame(bool captureDepth, bool captureBodyMask)
    : _captureDepth(captureDepth)
    , _captureBodyMask(captureBodyMask)
{
    _imageSizes[(int)AzureKinectImageType::Color] = FRAME_BUFSIZE_RGBA;
    _imageSizes[(int)AzureKinectImageType::Depth] = FRAME_BUFSIZE_DEPTH16;
    _imageSizes[(int)AzureKinectImageType::BodyMask] = FRAME_BUFSIZE_DEPTH16;

    for (int i = 0; i < AZURE_KINECT_IMAGE_TYPE_COUNT; i++)
    {
        _images[i] = new uint8_t[_imageSizes[i]];
    }
}

AzureKinectCameraFrame::~AzureKinectCameraFrame()
{
    for (int i = 0; i < AZURE_KINECT_IMAGE_TYPE_COUNT; i++)
    {
        delete[] _images[i];
    }
}

void AzureKinectCameraFrame::StageImage(AzureKinectImageType imageType, k4a_image_t image)
{
    auto stride = k4a_image_get_stride_bytes(image);
    auto buffer = k4a_image_get_buffer(image);
    rsize_t height = k4a_image_get_height_pixels(image);

    _imageStrides[(int)imageType] = stride;
    memcpy_s(_images[(int)imageType], _imageSizes[(int)imageType], buffer, height * stride);
}

void AzureKinectCameraFrame::UpdateSRV(AzureKinectImageType imageType, ID3D11Device* device, ID3D11ShaderResourceView* targetView)
{
    if (targetView != nullptr)
    {
        DirectXHelper::UpdateSRV(device, targetView, _images[(int)imageType], _imageStrides[(int)imageType]);
    }
}