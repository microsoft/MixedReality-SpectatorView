// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"

#if defined(INCLUDE_AZUREKINECT)
#include "AzureKinectFrameProvider.h"
#include <k4a/k4a.h>
#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
#include <k4abt.h>
#endif

AzureKinectFrameProvider::AzureKinectFrameProvider(ProviderType providerType)
    : _colorSRV(nullptr)
    , _depthSRV(nullptr)
    , _bodySRV(nullptr)
    , _providerType(providerType)
    , d3d11Device(nullptr)
    , cameraInput(nullptr)
{
}

HRESULT AzureKinectFrameProvider::Initialize(ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture)
{
    _depthSRV = depthSRV;
    _bodySRV = bodySRV;
    _colorSRV = colorSRV;
    _colorSRV->GetDevice(&d3d11Device);

    k4a_depth_mode_t depthCameraMode;
    switch (_providerType)
    {
    case AzureKinect_DepthCamera_Off:
        depthCameraMode = K4A_DEPTH_MODE_OFF;
        break;
    case AzureKinect_DepthCamera_NFOV:
        depthCameraMode = K4A_DEPTH_MODE_NFOV_UNBINNED;
        break;
    case AzureKinect_DepthCamera_WFOV:
        depthCameraMode = K4A_DEPTH_MODE_WFOV_2X2BINNED;
        break;
    default:
        depthCameraMode = K4A_DEPTH_MODE_OFF;
        break;
    }

    cameraInput = std::make_shared<AzureKinectCameraInput>(depthCameraMode, _depthSRV != nullptr && depthCameraMode != K4A_DEPTH_MODE_OFF, _bodySRV != nullptr && depthCameraMode != K4A_DEPTH_MODE_OFF);

    return S_OK;
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
    cameraInput->UpdateSRVs(compositeFrameIndex, d3d11Device, _colorSRV, _depthSRV, _bodySRV);
}

bool AzureKinectFrameProvider::IsEnabled()
{
    return cameraInput != nullptr;
}

bool AzureKinectFrameProvider::SupportsOutput()
{
    return false;
}

void AzureKinectFrameProvider::Dispose()
{
    cameraInput = nullptr;
}

bool AzureKinectFrameProvider::OutputYUV()
{
    return false;
}
#endif