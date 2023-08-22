// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "stdafx.h"
#include "CompositorShared.h"
#include <Windows.h>
#include <ppltasks.h>

#include "DirectXHelper.h"
#include "CompositorInterface.h"

#include "PluginAPI\IUnityInterface.h"
#include "PluginAPI\IUnityGraphics.h"
#include "PluginAPI\IUnityGraphicsD3D11.h"
#include "BufferedTextureFetch.h"


#define UNITYDLL EXTERN_C __declspec(dllexport)

static CompositorInterface* ci = nullptr;
static bool isRecording = false;
static bool videoInitialized = false;

static BYTE* colorBytes = new BYTE[FRAME_BUFSIZE_RGBA];
static BYTE* depthBytes = new BYTE[FRAME_BUFSIZE_DEPTH16];
static BYTE* bodyMaskBytes = new BYTE[FRAME_BUFSIZE_DEPTH16];
static BYTE* holoBytes = new BYTE[FRAME_BUFSIZE_RGBA];

static ID3D11Texture2D* g_holoRenderTexture = nullptr;

static ID3D11Texture2D* g_colorTexture = nullptr;
static ID3D11Texture2D* g_depthCameraTexture = nullptr;
static ID3D11Texture2D* g_bodyMaskTexture = nullptr;
static ID3D11Texture2D* g_compositeTexture = nullptr;
static ID3D11Texture2D* g_videoTexture = nullptr;
static ID3D11Texture2D* g_outputTexture = nullptr;

static ID3D11ShaderResourceView* g_UnityColorSRV = nullptr;
static ID3D11ShaderResourceView* g_UnityDepthSRV = nullptr;
static ID3D11ShaderResourceView* g_UnityBodySRV = nullptr;

static ID3D11Device* g_pD3D11Device = NULL;

static bool takePicture = false;
static bool takeRawPicture = false;
static std::wstring rawPicturePath;

static CRITICAL_SECTION lock;

static IUnityInterfaces *s_UnityInterfaces = nullptr;
static IUnityGraphics *s_Graphics = nullptr;

static int lastRecordedVideoFrame = -1;
static int lastVideoFrame = -1;
static BufferedTextureFetch VideoTextureBuffer;

static bool isInitialized = false;


static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    switch (eventType)
    {
    case kUnityGfxDeviceEventInitialize:
    {
        IUnityGraphicsD3D11* d3d11 = s_UnityInterfaces->Get<IUnityGraphicsD3D11>();
        if (d3d11 != nullptr)
        {
            g_pD3D11Device = d3d11->GetDevice();
        }
    }
    break;
    case kUnityGfxDeviceEventShutdown:
        VideoTextureBuffer.ReleaseTextures();
        g_pD3D11Device = NULL;
        break;
    }
}

void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces *unityInterfaces)
{
    InitializeCriticalSection(&lock);

    s_UnityInterfaces = unityInterfaces;
    s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

void UNITY_INTERFACE_API UnityPluginUnload()
{
    s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventShutdown);

    DeleteCriticalSection(&lock);

	if (ci != nullptr)
	{
		delete ci;
		ci = nullptr;
	}
}

UNITYDLL void UpdateCompositor()
{
    if (ci == NULL)
    {
        return;
    }

    //Update video encoder
    ci->Update();
}

static LONGLONG queuedVideoFrameTime;
static int queuedVideoFrameCount = 0;

void UpdateVideoRecordingFrame()
{
#if !HARDWARE_ENCODE_VIDEO
    //We have an old frame, lets get the data and queue it now
    if (VideoTextureBuffer.IsDataAvailable())
    {
        auto frame = ci->GetAvailableRecordFrame();
        VideoTextureBuffer.FetchTextureData(g_pD3D11Device, frame->Lock(), FRAME_BPP_RGBA);
        frame->timestamp = queuedVideoFrameTime;
        ci->RecordFrameAsync(std::move(frame), queuedVideoFrameCount);
    }
#endif

    if (lastVideoFrame >= 0 && lastRecordedVideoFrame != lastVideoFrame)
    {
#if _DEBUG
		std::wstring debugString = L"Updating the video recording texture, compositeFrameIndex: " + std::to_wstring(ci->compositeFrameIndex) + L", lastVideoFrame:" + std::to_wstring(lastVideoFrame) + L", lastRecordedVideoFrame: " + std::to_wstring(lastRecordedVideoFrame) + L"\n";
		OutputDebugString(debugString.data());
#endif

        queuedVideoFrameCount = ci->compositeFrameIndex - lastVideoFrame;
		if (queuedVideoFrameCount <= 0)
		{
#if _DEBUG
			debugString = L"compositeFrameIndex less than lastVideoFrame, updating queuedVideoFrameCount to be difference between lastVideoFrame and lastRecordedVideoFrame\n";
			OutputDebugString(debugString.data());
#endif
			queuedVideoFrameCount = lastVideoFrame - lastRecordedVideoFrame;
		}

		if (queuedVideoFrameCount <= 0)
		{
#if _DEBUG
			debugString = L"lastVideoFrame less than lastRecordedVideoFrame, setting queuedVideoFrameCount to one\n";
			OutputDebugString(debugString.data());
#endif
			queuedVideoFrameCount = 1;
		}

        lastRecordedVideoFrame = lastVideoFrame;
        queuedVideoFrameTime = lastVideoFrame * ci->GetColorDuration();
#if HARDWARE_ENCODE_VIDEO
        auto frame = ci->GetAvailableRecordFrame();
        frame->CopyFrom(g_videoTexture);
        frame->timestamp = queuedVideoFrameTime;
        ci->RecordFrameAsync(std::move(frame), queuedVideoFrameCount);
#else
        VideoTextureBuffer.PrepareTextureFetch(g_pD3D11Device, g_videoTexture);
#endif
    }

    lastVideoFrame = ci->compositeFrameIndex;
}

// Plugin function to handle a specific rendering event
static void __stdcall OnRenderEvent(int eventID)
{
    if (ci == nullptr)
    {
        return;
    }

    //  Update hologram texture from the spectator view camera.
    ci->UpdateFrameProvider();

    EnterCriticalSection(&lock);

    if (g_pD3D11Device != nullptr)
    {
        if (!videoInitialized && ci != nullptr)
        {
            videoInitialized = ci->InitializeVideoEncoder(g_pD3D11Device);
        }

        if (isRecording &&
            g_videoTexture != nullptr)
        {
            UpdateVideoRecordingFrame();
        }

        if (takePicture && g_colorTexture != nullptr)
        {
            takePicture = false;

            DirectXHelper::GetBytesFromTexture(g_pD3D11Device, g_compositeTexture, FRAME_BPP_RGBA, holoBytes);

            DirectXHelper::FlipHorizontally(holoBytes, FRAME_HEIGHT, FRAME_WIDTH * FRAME_BPP_RGBA);
            ci->TakePicture(g_pD3D11Device, FRAME_WIDTH, FRAME_HEIGHT, FRAME_BPP_RGBA, holoBytes);
        }

        if (takeRawPicture && g_colorTexture != nullptr)
        {
            takeRawPicture = false;

            DirectXHelper::GetBytesFromTexture(g_pD3D11Device, g_compositeTexture, FRAME_BPP_RGBA, holoBytes);

            std::ofstream strm;
            strm.open(rawPicturePath, std::ios::out | std::ios::binary | std::ios::trunc);
            strm.write((const char*)(holoBytes), FRAME_BUFSIZE_RGBA);
            strm.close();
        }
    }

    LeaveCriticalSection(&lock);
}

UNITYDLL LONGLONG GetColorDuration()
{
    if (ci != nullptr)
    {
        return ci->GetColorDuration();
    }

    return (LONGLONG)((1.0f / 30.0f) * QPC_MULTIPLIER);
}

UNITYDLL int GetCaptureFrameIndex()
{
    if (ci != nullptr)
    {
        return ci->GetCaptureFrameIndex();
    }

    return 0;
}

UNITYDLL int GetPixelChange(int frame)
{
    if (ci != nullptr)
    {
        return ci->GetPixelChange(frame);
    }

    return 0;
}

UNITYDLL int GetNumQueuedOutputFrames()
{
    if (ci != nullptr)
    {
        return ci->GetNumQueuedOutputFrames();
    }

    return 0;
}

UNITYDLL void SetLatencyPreference(float latencyPreference)
{
    if (ci != nullptr)
    {
        ci->SetLatencyPreference(latencyPreference);
    }
}

UNITYDLL void SetCompositeFrameIndex(int index)
{
    if (ci != nullptr)
        return ci->SetCompositeFrameIndex(index);
}

UNITYDLL bool IsCameraCalibrationInformationAvailable()
{
    if (ci != nullptr)
    {
        return ci->IsCameraCalibrationInformationAvailable();
    }
    else
    {
        return false;
    }
}

UNITYDLL void GetCameraCalibrationInformation(CameraIntrinsics* cameraIntrinsics)
{
    if (ci != nullptr)
    {
        ci->GetCameraCalibrationInformation(cameraIntrinsics);
    }
}

// Function to pass a callback to plugin-specific scripts
EXTERN_C UnityRenderingEvent __declspec(dllexport) __stdcall GetRenderEventFunc()
{
    return OnRenderEvent;
}

UNITYDLL int GetFrameWidth()
{
    return FRAME_WIDTH;
}

UNITYDLL int GetFrameHeight()
{
    return FRAME_HEIGHT;
}

UNITYDLL int GetVideoRecordingFrameWidth(VideoRecordingFrameLayout frameLayout)
{
    if (frameLayout == VideoRecordingFrameLayout::Quad)
    {
        return QUAD_FRAME_WIDTH;
    }
    else
    {
        return FRAME_WIDTH;
    }
}

UNITYDLL int GetVideoRecordingFrameHeight(VideoRecordingFrameLayout frameLayout)
{
    if (frameLayout == VideoRecordingFrameLayout::Quad)
    {
        return QUAD_FRAME_HEIGHT;
    }
    else
    {
        return FRAME_HEIGHT;
    }
}

UNITYDLL bool IsFrameProviderSupported(int providerId)
{
	if (ci == nullptr)
	{
		ci = new CompositorInterface();
	}

	return ci->IsFrameProviderSupported((IFrameProvider::ProviderType) providerId);
}

UNITYDLL bool IsOutputFrameProviderSupported(int providerId)
{
	if (ci == nullptr)
	{
		ci = new CompositorInterface();
	}

	return ci->IsOutputFrameProviderSupported((IFrameProvider::ProviderType) providerId);
}

UNITYDLL bool IsOcclusionSettingSupported(int setting)
{
    if (ci == nullptr)
    {
        ci = new CompositorInterface();
    }

    return ci->IsOcclusionSettingSupported((IFrameProvider::OcclusionSetting) setting);
}

UNITYDLL bool InitializeFrameProviderOnDevice(int providerId, int outputProviderId)
{
    if (g_outputTexture == nullptr ||
        g_UnityColorSRV == nullptr ||
        g_pD3D11Device == nullptr)
    {
        return false;
    }

    if (isInitialized)
    {
        return true;
    }

    if (ci == nullptr)
    {
        ci = new CompositorInterface();
    }

    ci->SetFrameProvider((IFrameProvider::ProviderType) providerId);
    ci->SetOutputFrameProvider((IFrameProvider::ProviderType) outputProviderId);
    isInitialized = ci->Initialize(g_pD3D11Device, g_UnityColorSRV, g_UnityDepthSRV, g_UnityBodySRV, g_outputTexture);

    return isInitialized;
}

UNITYDLL void StopFrameProvider()
{
    if (ci != NULL)
    {
        ci->StopFrameProvider();
    }
}

UNITYDLL void SetAudioData(BYTE* audioData, int audioSize, double audioTime)
{
    if (!isRecording)
    {
        return;
    }
    
#if ENCODE_AUDIO
    if (ci != nullptr)
    {
		LONGLONG audioTimeHNS = audioTime * QPC_MULTIPLIER;
        ci->RecordAudioFrameAsync(audioData, audioTimeHNS, audioSize);
    }
#endif    
}

UNITYDLL void TakePicture()
{
    takePicture = true;

}

UNITYDLL void TakeRawPicture(LPCWSTR lpFilePath)
{
    takeRawPicture = true;
    rawPicturePath = lpFilePath;
}

UNITYDLL bool StartRecording(VideoRecordingFrameLayout frameLayout, LPCWSTR lpcDesiredFileName, const int desiredFileNameLength, const int inputFileNameLength, LPWSTR lpFileName, int* fileNameLength)
{
    if (videoInitialized && ci != nullptr)
    {
        lastVideoFrame = -1;
		lastRecordedVideoFrame = -1;
        VideoTextureBuffer.ReleaseTextures();
        VideoTextureBuffer.Reset();
		isRecording = ci->StartRecording(frameLayout, lpcDesiredFileName, desiredFileNameLength, inputFileNameLength, lpFileName, fileNameLength);
		return isRecording;
    }

	return false;
}

UNITYDLL void StopRecording()
{
    if (videoInitialized && ci != nullptr)
    {
        ci->StopRecording();
        isRecording = false;
    }
}

UNITYDLL bool IsRecording()
{
    return isRecording;
}

UNITYDLL void SetAlpha(float alpha)
{
    if (ci != NULL)
    {
        ci->SetAlpha(alpha);
    }
}

UNITYDLL float GetAlpha()
{
    if (ci != NULL)
    {
        return ci->GetAlpha();
    }

    return 0;
}

UNITYDLL void Reset()
{
    EnterCriticalSection(&lock);
    g_colorTexture = nullptr;
    g_depthCameraTexture = nullptr;
    g_bodyMaskTexture = nullptr;
    g_compositeTexture = nullptr;
    g_videoTexture = nullptr;
    g_outputTexture = nullptr;

    g_holoRenderTexture = nullptr;

    g_UnityColorSRV = nullptr;
    g_UnityDepthSRV = nullptr;
    g_UnityBodySRV = nullptr;

    isInitialized = false;

    LeaveCriticalSection(&lock);
}

UNITYDLL bool ProvidesYUV()
{
    if (ci == nullptr)
    {
        return false;
    }

    return ci->ProvidesYUV();
}

UNITYDLL bool ExpectsYUV()
{
    if (ci == nullptr)
    {
        return false;
    }

    return ci->ExpectsYUV();
}

UNITYDLL LONGLONG GetCurrentUnityTime()
{
    LARGE_INTEGER time;
    QueryPerformanceCounter(&time);
    return time.QuadPart;
}

UNITYDLL bool HardwareEncodeVideo()
{
    return HARDWARE_ENCODE_VIDEO;
}

#pragma region CreateExternalTextures
UNITYDLL bool SetHoloTexture(ID3D11Texture2D* holoTexture)
{
    // We have already set a texture ptr.
    if (g_compositeTexture != nullptr)
    {
        return true;
    }

    if (g_compositeTexture == nullptr)
    {
		g_compositeTexture = holoTexture;
    }

    return g_compositeTexture != nullptr;
}

UNITYDLL bool SetVideoRenderTexture(ID3D11Texture2D* tex)
{
    g_videoTexture = tex;

    return g_videoTexture != nullptr;
}

UNITYDLL bool SetOutputRenderTexture(ID3D11Texture2D* tex)
{
    if (g_outputTexture == nullptr)
    {
        g_outputTexture = tex;
    }

    return g_outputTexture != nullptr;
}

UNITYDLL bool CreateUnityColorTexture(ID3D11ShaderResourceView*& srv)
{
    if (g_UnityColorSRV == nullptr && g_pD3D11Device != nullptr)
    {
        g_colorTexture = DirectXHelper::CreateTexture(g_pD3D11Device, colorBytes, FRAME_WIDTH, FRAME_HEIGHT, FRAME_BPP_RGBA);

        if (g_colorTexture == nullptr)
        {
            return false;
        }

        g_UnityColorSRV = DirectXHelper::CreateShaderResourceView(g_pD3D11Device, g_colorTexture);
        if (g_UnityColorSRV == nullptr)
        {
            return false;
        }
    }

    srv = g_UnityColorSRV;
    return true;
}

UNITYDLL bool CreateUnityDepthCameraTexture(ID3D11ShaderResourceView*& srv)
{
    if (g_UnityDepthSRV == nullptr && g_pD3D11Device != nullptr)
    {
        g_depthCameraTexture = DirectXHelper::CreateTexture(g_pD3D11Device, depthBytes, FRAME_WIDTH, FRAME_HEIGHT, FRAME_BPP_DEPTH16, DXGI_FORMAT_R16_UNORM);

        if (g_depthCameraTexture == nullptr)
        {
            return false;
        }

        g_UnityDepthSRV = DirectXHelper::CreateShaderResourceView(g_pD3D11Device, g_depthCameraTexture, DXGI_FORMAT_R16_UNORM);
        if (g_UnityDepthSRV == nullptr)
        {
            return false;
        }
    }

    srv = g_UnityDepthSRV;
    return true;
}

UNITYDLL bool CreateUnityBodyMaskTexture(ID3D11ShaderResourceView*& srv)
{
    if (g_UnityBodySRV == nullptr && g_pD3D11Device != nullptr)
    {
        g_bodyMaskTexture = DirectXHelper::CreateTexture(g_pD3D11Device, bodyMaskBytes, FRAME_WIDTH, FRAME_HEIGHT, FRAME_BPP_DEPTH16, DXGI_FORMAT_R16_UNORM);

        if (g_bodyMaskTexture == nullptr)
        {
            return false;
        }

        g_UnityBodySRV = DirectXHelper::CreateShaderResourceView(g_pD3D11Device, g_bodyMaskTexture, DXGI_FORMAT_R16_UNORM);
        if (g_UnityBodySRV == nullptr)
        {
            return false;
        }
    }

    srv = g_UnityBodySRV;
    return true;
}


UNITYDLL bool IsArUcoMarkerDetectorSupported()
{
    if (ci != nullptr)
    {
        return ci->IsArUcoMarkerDetectorSupported();
    }
    else
    {
        return false;
    }
}

UNITYDLL void StartArUcoMarkerDetector(cv::aruco::PREDEFINED_DICTIONARY_NAME markerDictionaryName, float markerSize)
{
    if (ci != nullptr)
    {
        ci->StartArUcoMarkerDetector(markerDictionaryName, markerSize);
    }
}

UNITYDLL void StopArUcoMarkerDetector()
{
    if (ci != nullptr)
    {
        ci->StopArUcoMarkerDetector();
    }
}

UNITYDLL int GetLatestArUcoMarkerCount()
{
    if (ci != nullptr)
    {
        return ci->GetLatestArUcoMarkerCount();
    }
    else
    {
        return 0;
    }
}

UNITYDLL void GetLatestArUcoMarkers(int size, Marker* markers)
{
    if (ci != nullptr)
    {
        ci->GetLatestArUcoMarkers(size, markers);
    }
}
#pragma endregion CreateExternalTextures
