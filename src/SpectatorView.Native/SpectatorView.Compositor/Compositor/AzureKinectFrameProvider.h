// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#include "CompositorInterface.h"
#include "IFrameProvider.h"
#include "ArUcoMarkerDetector.h"
#if defined(INCLUDE_AZUREKINECT)
#include <opencv2\aruco.hpp>
#include <k4a/k4a.h>
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
#include <k4abt.h>
#endif
class AzureKinectFrameProvider : public IFrameProvider
{
public:

    AzureKinectFrameProvider(ProviderType providerType);

    // Inherited via IFrameProvider
    virtual HRESULT Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture) override;
    virtual LONGLONG GetTimestamp(int frame) override;
    virtual LONGLONG GetDurationHNS() override;
    virtual void Update(int compositeFrameIndex) override;
    virtual ProviderType GetProviderType() override;
    virtual bool IsEnabled() override;
    virtual bool SupportsOutput() override;
    virtual void Dispose() override;
    virtual bool OutputYUV() override;

    virtual int GetCaptureFrameIndex() override
    {
        return _captureFrameIndex;
    }
   
   virtual bool IsCameraCalibrationInformationAvailable() override
    {
        return true;
    }

    virtual void GetCameraCalibrationInformation(CameraIntrinsics* calibration) override;

    virtual bool IsArUcoMarkerDetectorSupported() override
    {
        return true;
    }

    virtual void StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize) override;
    virtual void StopArUcoMarkerDetector() override;
    virtual int GetLatestArUcoMarkerCount() override { return markerDetector->GetDetectedMarkersCount(); }
    virtual void GetLatestArUcoMarkers(int size, Marker* markers) override;

private:
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    void GetBodyIndexMap(k4a_capture_t capture, k4abt_frame_t* bodyFrame, k4a_image_t* bodyIndexMap, uint8_t** bodyIndexBuffer);
    void ReleaseBodyIndexMap(k4abt_frame_t bodyFrame, k4a_image_t bodyIndexMap);
#endif
    void UpdateSRV(k4a_image_t bodyDepthImage, ID3D11ShaderResourceView* _srv);
    void UpdateArUcoMarkers(k4a_image_t image);
    void SetBodyMaskBuffer(uint16_t* bodyMaskBuffer, uint8_t* bodyIndexBuffer, int bufferSize);
    void SetTransformedBodyMaskBuffer(uint16_t* transformedBodyMaskBuffer, int bufferSize);

    int _captureFrameIndex;
    ID3D11ShaderResourceView* _colorSRV;
    ID3D11ShaderResourceView* _depthSRV;
    ID3D11ShaderResourceView* _bodySRV;
    ID3D11Device* d3d11Device;
    k4a_device_t k4aDevice;
    k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
    k4a_calibration_t calibration;
    k4a_transformation_t transformation;
    k4a_image_t transformedDepthImage;
    k4a_image_t transformedBodyMaskImage;
    k4a_image_t bodyMaskImage;
    CRITICAL_SECTION lock;
    bool detectMarkers;
    float markerSize;
    cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName;
    k4a_depth_mode_t depthCameraMode = K4A_DEPTH_MODE_OFF;

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    k4abt_tracker_t k4abtTracker;
    k4abt_tracker_configuration_t tracker_config = K4ABT_TRACKER_CONFIG_DEFAULT;
#endif

    std::shared_ptr<ArUcoMarkerDetector> markerDetector;
};
#endif