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

// Represents a single frame from the AzureKinect camera,
// bundling together the color image, the depth image, and
// the body mask image for that frame.
class AzureKinectCameraFrame
{
public:
    AzureKinectCameraFrame(bool captureDepth, bool captureBodyMask);
    ~AzureKinectCameraFrame();

    void StageImage(AzureKinectImageType imageType, k4a_image_t image);
    void UpdateSRV(AzureKinectImageType imageType, ID3D11Device* device, ID3D11ShaderResourceView* targetView);

    bool TryBeginWritingColorAndDepth();
    void EndWritingColorAndDepth();
    void BeginWritingBodyMask();
    void EndWritingBodyMask();
    bool TryBeginReading();
    void EndReading();

private:
    enum class FrameStatus
    {
        // Marks a frame that is currently unusued, and is ready to be written
        // to by the camera.
        Unused,

        // Marks a frame that is currently in the process of having its color
        // and depth images captured and staged.
        WritingColorAndDepth,

        // Marks a frame that is currently in the process of having its body
        // mask captured and staged.
        WritingBodyMask,

        // Marks a frame that is fully-staged and ready to be read from.
        // Staged frames can be overwritten when the reading thread is not
        // consuming frames (e.g. if the Unity player is paused but the
        // camera input is still pulling frames).
        Staged,

        // Marks a frame that is currently being read from and will soon
        // be released to the unused state.
        Reading
    };

    uint8_t* _images[AZURE_KINECT_IMAGE_TYPE_COUNT] = { nullptr };
    int _imageSizes[AZURE_KINECT_IMAGE_TYPE_COUNT] = { 0 };
    int _imageStrides[AZURE_KINECT_IMAGE_TYPE_COUNT] = { 0 };

    bool _captureDepth;
    bool _captureBodyMask;

    std::mutex _statusGuard;
    FrameStatus _status;
};

#endif