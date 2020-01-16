// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

#include "IFrameProvider.h"
#include <k4a/k4a.h>

class AzureKinectFrameProvider : public IFrameProvider
{
public:
    // Inherited via IFrameProvider
    virtual HRESULT Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11Texture2D* outputTexture) override;
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

private:
    int _captureFrameIndex;
    ID3D11ShaderResourceView* _colorSRV;
    ID3D11ShaderResourceView* _depthSRV;
    ID3D11Device* d3d11Device;
    k4a_device_t k4aDevice;
    k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
    k4a_calibration_t calibration;
    k4a_transformation_t transformation;
    k4a_image_t transformedDepthImage;
    CRITICAL_SECTION lock;
};