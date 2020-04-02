// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#include "CompositorInterface.h"
#include "IFrameProvider.h"
#include "ArUcoMarkerDetector.h"
#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectCameraInput.h"

class AzureKinectFrameProvider : public IFrameProvider
{
public:

    AzureKinectFrameProvider(ProviderType providerType);

    // Inherited via IFrameProvider
    virtual HRESULT Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture) override;
    virtual LONGLONG GetTimestamp(int frame) override;
    virtual LONGLONG GetDurationHNS() override;
    virtual void Update(int compositeFrameIndex) override;
    virtual ProviderType GetProviderType() override { return _providerType; }
    virtual bool IsEnabled() override;
    virtual bool SupportsOutput() override;
    virtual void Dispose() override;
    virtual bool OutputYUV() override;

    virtual int GetCaptureFrameIndex() override
    {
        return cameraInput == nullptr ? 0 : cameraInput->GetCaptureFrameIndex();
    }
   
   virtual bool IsCameraCalibrationInformationAvailable() override
    {
        return true;
    }

   virtual void GetCameraCalibrationInformation(CameraIntrinsics* calibration) override { cameraInput->GetCameraCalibrationInformation(calibration); }

    virtual bool IsArUcoMarkerDetectorSupported() override
    {
        return true;
    }

    virtual void StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize) override { cameraInput->StartArUcoMarkerDetector(markerDictionaryName, markerSize); }
    virtual void StopArUcoMarkerDetector() override { cameraInput->StopArUcoMarkerDetector(); }
    virtual int GetLatestArUcoMarkerCount() override { return cameraInput->GetLatestArUcoMarkerCount(); }
    virtual void GetLatestArUcoMarkers(int size, Marker* markers) override { cameraInput->GetLatestArUcoMarkers(size, markers); }

private:
    std::shared_ptr<AzureKinectCameraInput> cameraInput;

    ProviderType _providerType;
    ID3D11ShaderResourceView* _colorSRV;
    ID3D11ShaderResourceView* _depthSRV;
    ID3D11ShaderResourceView* _bodySRV;
    ID3D11Device* d3d11Device;
};
#endif