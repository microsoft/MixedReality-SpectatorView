// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#if defined(INCLUDE_AZUREKINECT)

#include <k4a/k4a.h>
#define AZURE_KINECT_IMAGE_TYPE_COUNT 3

enum class AzureKinectImageType
{
    Color = 0,
    Depth = 1,
    BodyMask = 2
};

class AzureKinectCameraFrame
{
public:
    AzureKinectCameraFrame(bool captureDepth, bool captureBodyMask);
    ~AzureKinectCameraFrame();

    void StageImage(AzureKinectImageType imageType, k4a_image_t image);
    void UpdateSRV(AzureKinectImageType imageType, ID3D11Device* device, ID3D11ShaderResourceView* targetView);

private:
    uint8_t* _images[AZURE_KINECT_IMAGE_TYPE_COUNT] = { nullptr };
    int _imageSizes[AZURE_KINECT_IMAGE_TYPE_COUNT];
    int _imageStrides[AZURE_KINECT_IMAGE_TYPE_COUNT] = { 0 };
    bool _captureDepth;
    bool _captureBodyMask;
};

#endif