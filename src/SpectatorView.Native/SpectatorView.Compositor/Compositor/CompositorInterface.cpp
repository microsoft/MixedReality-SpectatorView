// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "CompositorInterface.h"
#include "codecapi.h"
#include "AzureKinectFrameProvider.h"

CompositorInterface::CompositorInterface()
{
    wchar_t myDocumentsPath[1024];
    SHGetFolderPathW(0, CSIDL_MYDOCUMENTS, 0, 0, myDocumentsPath);
    outputPath = std::wstring(myDocumentsPath) + L"\\HologramCapture\\";

    DirectoryHelper::CreateOutputDirectory(outputPath);

    frameProvider = NULL;
}

void CompositorInterface::SetFrameProvider(IFrameProvider::ProviderType type)
{
    DisableOutputFrameProvider();
    if (frameProvider)
    {
        if (frameProvider->GetProviderType() == type)
            return;
        frameProvider->Dispose();
        delete frameProvider;
        frameProvider = NULL;
    }

#if defined(INCLUDE_ELGATO)
    if(type == IFrameProvider::ProviderType::Elgato)
        frameProvider = new ElgatoFrameProvider();
#endif

#if defined(INCLUDE_BLACKMAGIC)
    if (type == IFrameProvider::ProviderType::BlackMagic)
        frameProvider = new DeckLinkManager();
#endif

#if defined(INCLUDE_AZUREKINECT)
    if (type == IFrameProvider::ProviderType::AzureKinect_DepthCamera_Off)
        frameProvider = new AzureKinectFrameProvider(IFrameProvider::ProviderType::AzureKinect_DepthCamera_Off);
    else if (type == IFrameProvider::ProviderType::AzureKinect_DepthCamera_NFOV)
        frameProvider = new AzureKinectFrameProvider(IFrameProvider::ProviderType::AzureKinect_DepthCamera_NFOV);
    else if (type == IFrameProvider::ProviderType::AzureKinect_DepthCamera_WFOV)
        frameProvider = new AzureKinectFrameProvider(IFrameProvider::ProviderType::AzureKinect_DepthCamera_WFOV);
#endif
}

void CompositorInterface::SetOutputFrameProvider(IFrameProvider::ProviderType type)
{
    DisableOutputFrameProvider();

    // There is no need for a second instance of the same provider type
    IFrameProvider::ProviderType inputProviderType = IFrameProvider::ProviderType::None;
    if (frameProvider != nullptr)
        inputProviderType = frameProvider->GetProviderType();
    if (inputProviderType == type)
        return;

#if defined (INCLUDE_BLACKMAGIC)
    if (type == IFrameProvider::ProviderType::BlackMagic)
        outputFrameProvider = new DeckLinkManager();
#endif
}

void CompositorInterface::DisableOutputFrameProvider()
{
    if (outputFrameProvider == nullptr)
        return;
    outputFrameProvider->Dispose();
    delete outputFrameProvider;
    outputFrameProvider = nullptr;
}

bool CompositorInterface::IsFrameProviderSupported(IFrameProvider::ProviderType providerType)
{
#if defined(INCLUDE_ELGATO)
	if (providerType == IFrameProvider::ProviderType::Elgato)
		return true;
#endif

#if defined(INCLUDE_BLACKMAGIC)
	if (providerType == IFrameProvider::ProviderType::BlackMagic)
		return true;
#endif

#if defined(INCLUDE_AZUREKINECT)
    if (providerType == IFrameProvider::ProviderType::AzureKinect_DepthCamera_Off
        || providerType == IFrameProvider::ProviderType::AzureKinect_DepthCamera_NFOV
        || providerType == IFrameProvider::ProviderType::AzureKinect_DepthCamera_WFOV)
        return true;
#endif

	return false;
}

bool CompositorInterface::IsOutputFrameProviderSupported(IFrameProvider::ProviderType providerType)
{
#if defined(INCLUDE_BLACKMAGIC)
    if (providerType == IFrameProvider::ProviderType::BlackMagic)
        return true;
#endif
    if (providerType == IFrameProvider::ProviderType::None)
        return true;
    return false;
}

bool CompositorInterface::IsOcclusionSettingSupported(IFrameProvider::OcclusionSetting setting)
{
#if defined(INCLUDE_AZUREKINECT)
    if (setting == IFrameProvider::OcclusionSetting::RawDepthCamera)
        return true;

#if defined(INCLUDE_AZUREKINECT_BODYTRACKING)
    if (setting == IFrameProvider::OcclusionSetting::BodyTracking)
        return true;
#endif
#endif
    return false;
}

bool CompositorInterface::IsCameraCalibrationInformationAvailable()
{
    if (frameProvider == nullptr)
    {
        return false;
    }

    return frameProvider->IsCameraCalibrationInformationAvailable();
}

void CompositorInterface::GetCameraCalibrationInformation(CameraIntrinsics* calibration)
{
    if (frameProvider != nullptr)
    {
        frameProvider->GetCameraCalibrationInformation(calibration);
    }
}

bool CompositorInterface::Initialize(ID3D11Device* device, ID3D11ShaderResourceView* colorSRV, ID3D11ShaderResourceView* depthSRV, ID3D11ShaderResourceView* bodySRV, ID3D11Texture2D* outputTexture)
{
    if (frameProvider == nullptr)
    {
        return false;
    }

    if (frameProvider->IsEnabled())
    {
        return true;
    }

    _device = device;

    if (outputFrameProvider == nullptr)
        return SUCCEEDED(frameProvider->Initialize(colorSRV, depthSRV, bodySRV, outputTexture));
    else
    {
        return
            SUCCEEDED(frameProvider->Initialize(colorSRV, depthSRV, bodySRV, nullptr)) &&
            SUCCEEDED(outputFrameProvider->Initialize(nullptr, nullptr, nullptr, outputTexture));
    }
}

bool CompositorInterface::IsArUcoMarkerDetectorSupported()
{
    if (frameProvider != nullptr)
    {
        return frameProvider->IsArUcoMarkerDetectorSupported();
    }
    else
    {
        return false;
    }
}

void CompositorInterface::StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize)
{
    if (frameProvider != nullptr)
    {
        frameProvider->StartArUcoMarkerDetector(markerDictionaryName, markerSize);
    }
}

void CompositorInterface::StopArUcoMarkerDetector()
{
    if (frameProvider != nullptr)
    {
        frameProvider->StopArUcoMarkerDetector();
    }
}

int CompositorInterface::GetLatestArUcoMarkerCount()
{
    if (frameProvider == nullptr)
    {
        return 0;
    }
    else
    {
        return frameProvider->GetLatestArUcoMarkerCount();
    }
}

void CompositorInterface::GetLatestArUcoMarkers(int size, Marker* markers)
{
    if (frameProvider != nullptr)
    {
        return frameProvider->GetLatestArUcoMarkers(size, markers);
    }
}

void CompositorInterface::UpdateFrameProvider()
{
    if (frameProvider != nullptr)
    {
        frameProvider->Update(compositeFrameIndex);
    }
    if (outputFrameProvider != nullptr)
    {
        outputFrameProvider->Update(compositeFrameIndex);
    }
}

void CompositorInterface::Update()
{
    if (activeVideoEncoder != nullptr)
    {
        activeVideoEncoder->Update();
    }
}

void CompositorInterface::StopFrameProvider()
{
    if (frameProvider != nullptr)
    {
        frameProvider->Dispose();
    }
    DisableOutputFrameProvider();
}

LONGLONG CompositorInterface::GetTimestamp(int frame)
{
    if (frameProvider != nullptr)
    {
        return frameProvider->GetTimestamp(frame);
    }

    return INVALID_TIMESTAMP;
}

LONGLONG CompositorInterface::GetColorDuration()
{
    if (frameProvider != nullptr)
    {
        return frameProvider->GetDurationHNS();
    }

    return (LONGLONG)((1.0f / 30.0f) * QPC_MULTIPLIER);
}

int CompositorInterface::GetCaptureFrameIndex()
{
    if (frameProvider != nullptr)
    {
        return frameProvider->GetCaptureFrameIndex();
    }

    return 0;
}

int CompositorInterface::GetPixelChange(int frame)
{
    if (frameProvider != nullptr)
    {
        return frameProvider->GetPixelChange(frame);
    }

    return 0;
}

int CompositorInterface::GetNumQueuedOutputFrames()
{
    if (outputFrameProvider != nullptr)
        return outputFrameProvider->GetNumQueuedOutputFrames();

    if (frameProvider != nullptr)
    {
        return frameProvider->GetNumQueuedOutputFrames();
    }

    return 0;
}

void CompositorInterface::SetLatencyPreference(float latencyPreference)
{
    if (frameProvider != nullptr)
    {
        frameProvider->SetLatencyPreference(latencyPreference);
    }
    if (outputFrameProvider != nullptr)
    {
        outputFrameProvider->SetLatencyPreference(latencyPreference);
    }
}

void CompositorInterface::SetCompositeFrameIndex(int index)
{
    compositeFrameIndex = index;
}

void CompositorInterface::TakePicture(ID3D11Device* device, int width, int height, int bpp, BYTE* bytes)
{
    if (device == nullptr)
    {
        return;
    }

    ID3D11DeviceContext* context;
    device->GetImmediateContext(&context);

    photoIndex++;
    std::wstring photoPath = DirectoryHelper::FindUniqueFileName(outputPath, L"Photo", L".png", photoIndex);

    ID3D11Texture2D* tex = DirectXHelper::CreateTexture(device, bytes, width, height, bpp);
    DirectX::SaveWICTextureToFile(context, tex, GUID_ContainerFormatPng, photoPath.c_str());
}

bool CompositorInterface::InitializeVideoEncoder(ID3D11Device* device)
{
    videoEncoder1080p = new VideoEncoder(FRAME_WIDTH, FRAME_HEIGHT, FRAME_WIDTH * FRAME_BPP_RGBA, VIDEO_FPS,
        AUDIO_SAMPLE_RATE, AUDIO_CHANNELS, AUDIO_BPS, VIDEO_BITRATE_1080P, VIDEO_MPEG_LEVEL_1080P);

    videoEncoder4K = new VideoEncoder(QUAD_FRAME_WIDTH, QUAD_FRAME_HEIGHT, QUAD_FRAME_WIDTH * FRAME_BPP_RGBA, VIDEO_FPS,
        AUDIO_SAMPLE_RATE, AUDIO_CHANNELS, AUDIO_BPS, VIDEO_BITRATE_4K, VIDEO_MPEG_LEVEL_4K);

    return videoEncoder1080p->Initialize(device) && videoEncoder4K->Initialize(device);
}

bool CompositorInterface::StartRecording(VideoRecordingFrameLayout frameLayout, LPCWSTR lpcDesiredFileName, const int desiredFileNameLength, const int inputFileNameLength, LPWSTR lpFileName, int* fileNameLength)
{
	*fileNameLength = 0;

	std::wstring desiredFileName(lpcDesiredFileName);
	std::wstring extension(L".mp4");
	if (!DirectoryHelper::TestFileExtension(desiredFileName, extension))
	{
		return false;
	}

	std::wstring videoPath = DirectoryHelper::FindUniqueFileName(desiredFileName, extension);

	std::shared_lock<std::shared_mutex> lock(encoderLock);
	if (frameLayout == VideoRecordingFrameLayout::Composite)
	{
		activeVideoEncoder = videoEncoder1080p;
	}
	else
	{
		activeVideoEncoder = videoEncoder4K;
	}

	if (activeVideoEncoder == nullptr)
	{
		return false;
	}

	activeVideoEncoder->StartRecording(videoPath.c_str(), ENCODE_AUDIO);

	memcpy_s(lpFileName, inputFileNameLength * sizeof(wchar_t), videoPath.c_str(), videoPath.size() * sizeof(wchar_t));
	*fileNameLength = static_cast<int>(videoPath.size());
	return true;
}

void CompositorInterface::StopRecording()
{
	std::shared_lock<std::shared_mutex> lock(encoderLock);
    if (activeVideoEncoder == nullptr)
    {
        return;
    }

    activeVideoEncoder->StopRecording();
    activeVideoEncoder = nullptr;
}

std::unique_ptr<VideoEncoder::VideoInput> CompositorInterface::GetAvailableRecordFrame()
{
    if (activeVideoEncoder == nullptr)
    {
        OutputDebugString(L"GetAvailableRecordFrame dropped, no active encoder\n");
        return nullptr;
    }
    return activeVideoEncoder->GetAvailableVideoFrame();
}

void CompositorInterface::RecordFrameAsync(std::unique_ptr<VideoEncoder::VideoInput> frame, int numFrames)
{
#if _DEBUG
	std::wstring debugString = L"RecordFrameAsync called, frameTime:" + std::to_wstring(frame->timestamp) + L", numFrames:" + std::to_wstring(numFrames) + L"\n";
	OutputDebugString(debugString.data());
#endif

	std::shared_lock<std::shared_mutex> lock(encoderLock);
    if (frameProvider == nullptr || activeVideoEncoder == nullptr)
    {
		OutputDebugString(L"RecordFrameAsync dropped, no active frame provider or encoder\n");
        return;
    }
    if (numFrames < 1) 
        numFrames = 1;
    else if (numFrames > 5) 
        numFrames = 5;

	// The encoder will update sample times internally based on the first seen sample time when recording.
	// The encoder, however, does assume that audio and video samples will be based on the same source time.
	// Providing audio and video samples with different starting times will cause issues in the generated video file.
    frame->duration = numFrames * frameProvider->GetDurationHNS();
    activeVideoEncoder->QueueVideoFrame(std::move(frame));
}

void CompositorInterface::RecordAudioFrameAsync(BYTE* audioFrame, LONGLONG audioTime, int audioSize)
{
#if _DEBUG
	std::wstring debugString = L"RecordAudioFrameAsync called, audioTime:" + std::to_wstring(audioTime) + L", audioSize:" + std::to_wstring(audioSize) + L"\n";
	OutputDebugString(debugString.data());
#endif

	std::shared_lock<std::shared_mutex> lock(encoderLock);
    if (activeVideoEncoder == nullptr)
    {
#if _DEBUG
		OutputDebugString(L"RecordAudioFrameAsync dropped, no active encoder\n");
#endif
        return;
    }

	// The encoder will update sample times internally based on the first seen sample time when recording.
	// The encoder, however, does assume that audio and video samples will be based on the same source time.
	// Providing audio and video samples with different starting times will cause issues in the generated video file.
    auto frame = activeVideoEncoder->GetAvailableAudioFrame();
    frame->SetData(audioFrame, audioSize, audioTime);
    activeVideoEncoder->QueueAudioFrame(std::move(frame));
}

bool CompositorInterface::ProvidesYUV()
{
    if (frameProvider == nullptr)
    {
        return false;
    }

    return frameProvider->ProvidesYUV();
}

bool CompositorInterface::ExpectsYUV()
{
    if (outputFrameProvider != nullptr)
        return outputFrameProvider->ExpectsYUV();
    if (frameProvider != nullptr)
        return frameProvider->ExpectsYUV();

    return false;
}
