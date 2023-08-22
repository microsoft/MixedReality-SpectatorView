// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once
#include "pch.h"
#include "VideoEncoder.h"
#include "DirectoryHelper.h"
#include "Shlobj.h" // To get MyDocuments path
#include "ScreenGrab.h"
#include "DataStructures.h"
#include "wincodec.h"

#define DLLEXPORT __declspec(dllexport)

enum class VideoRecordingFrameLayout
{
    Composite = 0,
    Quad = 1
};

class CompositorInterface
{
private:
    IFrameProvider* frameProvider;
    IFrameProvider* outputFrameProvider = nullptr;
    float alpha = 0.9f;

    VideoEncoder* videoEncoder1080p = nullptr;
    VideoEncoder* videoEncoder4K = nullptr;
    VideoEncoder* activeVideoEncoder = nullptr;

    int photoIndex = -1;

    std::wstring outputPath, channelPath;

    ID3D11Device* _device;

    LONGLONG stubVideoTime = 0;

	// Audio write calls may occur off the main thread so we need to lock around encoder access.
	std::shared_mutex encoderLock;

public:
    DLLEXPORT CompositorInterface();
    DLLEXPORT void SetFrameProvider(IFrameProvider::ProviderType type);
    DLLEXPORT void SetOutputFrameProvider(IFrameProvider::ProviderType type);
    DLLEXPORT void DisableOutputFrameProvider();
	DLLEXPORT bool IsFrameProviderSupported(IFrameProvider::ProviderType providerType);
    DLLEXPORT bool IsOutputFrameProviderSupported(IFrameProvider::ProviderType providerType);
    DLLEXPORT bool IsOcclusionSettingSupported(IFrameProvider::OcclusionSetting setting);
    DLLEXPORT bool IsCameraCalibrationInformationAvailable();
    DLLEXPORT void GetCameraCalibrationInformation(CameraIntrinsics* cameraIntrinsics);

    DLLEXPORT bool IsArUcoMarkerDetectorSupported();
    DLLEXPORT void StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize);
    DLLEXPORT void StopArUcoMarkerDetector();
    DLLEXPORT int GetLatestArUcoMarkerCount();
    DLLEXPORT void GetLatestArUcoMarkers(int size, Marker* markers);

    DLLEXPORT bool Initialize(ID3D11Device* device, ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture);

    DLLEXPORT void UpdateFrameProvider();
    DLLEXPORT void Update();
    DLLEXPORT void StopFrameProvider();

    DLLEXPORT LONGLONG GetTimestamp(int frame);

    DLLEXPORT LONGLONG GetColorDuration();
    DLLEXPORT int GetCaptureFrameIndex();
    DLLEXPORT int GetPixelChange(int frame);
    DLLEXPORT int GetNumQueuedOutputFrames();
    DLLEXPORT void SetLatencyPreference(float latencyPreference);

    DLLEXPORT void SetCompositeFrameIndex(int index);

    DLLEXPORT void TakePicture(ID3D11Device* device, int width, int height, int bpp, BYTE* bytes);

    DLLEXPORT bool InitializeVideoEncoder(ID3D11Device* device);
    DLLEXPORT bool StartRecording(VideoRecordingFrameLayout frameLayout, LPCWSTR lpcDesiredFileName, const int desiredFileNameLength, const int inputFileNameLength, LPWSTR lpFileName, int* fileNameLength);
    DLLEXPORT void StopRecording();
    
	// frameTime is in hundred nano seconds
    DLLEXPORT std::unique_ptr<VideoEncoder::VideoInput> GetAvailableRecordFrame();
	DLLEXPORT void RecordFrameAsync(std::unique_ptr<VideoEncoder::VideoInput>, int numFrames);

	// audioTime is in hundrend nano seconds
    DLLEXPORT void RecordAudioFrameAsync(BYTE* audioFrame, LONGLONG audioTime, int audioSize);

    DLLEXPORT void SetAlpha(float newAlpha)
    {
        alpha = newAlpha;
    }

    DLLEXPORT float GetAlpha()
    {
        return alpha;
    }

    DLLEXPORT bool ProvidesYUV();
    DLLEXPORT bool ExpectsYUV();

public:
    int compositeFrameIndex;
};

