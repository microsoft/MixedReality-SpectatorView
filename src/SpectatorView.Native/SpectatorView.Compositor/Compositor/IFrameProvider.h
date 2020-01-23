// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#include "CameraIntrinsics.h"

class IFrameProvider
{
public:

    enum ProviderType
    {
        BlackMagic,
        Elgato,
        AzureKinect,
    };

    // Set up the FrameProvider to start delivering frames.
    virtual HRESULT Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11Texture2D* outputTexture) = 0;

    // 4 frames are caches for reliable hologram stability:
    // Get the timestamp of the currently rendered cached frame.
    virtual LONGLONG GetTimestamp(int frame) = 0;

    virtual LONGLONG GetDurationHNS() = 0;

    virtual void Update(int compositeFrameIndex) = 0;

    virtual ProviderType GetProviderType() = 0;

    // Check if the FrameProvider is currently delivering frames.
    virtual bool IsEnabled() = 0;
    // Check if this capture device supports outputting video.
    virtual bool SupportsOutput() = 0;
    // Stop capturing frames from the FrameProvider.
    virtual void Dispose() = 0;

    // Return true if function outputs YUV frames, false otherwise.
    virtual bool OutputYUV() = 0;

    virtual int GetCaptureFrameIndex() { return 0; }
    virtual int GetPixelChange(int frame) { return 0; }
    virtual int GetNumQueuedOutputFrames() { return 0; }
    virtual bool IsCameraCalibrationInformationAvailable() { return false; }
    virtual void GetCameraCalibrationInformation(CameraIntrinsics* calibration) {}

    virtual bool IsArUcoMarkerDetectorSupported() { return false; }
    virtual void StartArUcoMarkerDetector(float markerSize) {}
    virtual void StopArUcoMarkerDetector() {}
    virtual int GetLatestArUcoMarkerCount() { return 0; }
    virtual void GetLatestArUcoMarkers(int size, Marker* markers) { }
};
