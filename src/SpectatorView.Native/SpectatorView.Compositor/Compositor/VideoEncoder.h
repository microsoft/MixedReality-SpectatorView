// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// IMFSinkWriter:
// https://msdn.microsoft.com/en-us/library/windows/desktop/ff819477(v=vs.85).aspx

#pragma once

#include <Windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <Mfreadwrite.h>
#include <mferror.h>
#include <shared_mutex>

#include "DirectXHelper.h"

#include <queue>

#pragma comment(lib, "mf")
#pragma comment(lib, "mfreadwrite")
#pragma comment(lib, "mfplat")
#pragma comment(lib, "mfuuid")

#define INVALID_TIMESTAMP -1

class VideoEncoder
{
public:
    VideoEncoder(UINT frameWidth, UINT frameHeight, UINT frameStride, UINT fps,
        UINT32 audioSampleRate, UINT32 audioChannels, UINT32 audioBPS, UINT32 videoBitrate, UINT32 videoMpegLevel);
    ~VideoEncoder();

    bool Initialize(ID3D11Device* device);

    void StartRecording(LPCWSTR videoPath, bool encodeAudio = false);
    bool IsRecording();
    void StopRecording();

    // Used for recording video from a background thread.
    class VideoInput;
    std::unique_ptr<VideoInput> GetAvailableVideoFrame();
    void QueueVideoFrame(std::unique_ptr<VideoInput> frame);
    void QueueAudioFrame(byte* buffer, int bufferSize, LONGLONG timestamp);

    // Do not call this from a background thread.
    void Update();

    class VideoInputBase
    {
    public:
        IMFMediaBuffer* mediaBuffer = nullptr;
        LONGLONG timestamp = INVALID_TIMESTAMP;
        LONGLONG duration = INVALID_TIMESTAMP;
    };

#ifdef HARDWARE_ENCODE_VIDEO
    class VideoInput : public VideoInputBase
    {
        ID3D11Device* device;
        ID3D11DeviceContext* deviceContext;
        ID3D11Texture2D* texture = nullptr;
    public:
        VideoInput(ID3D11Device* _device) : device(_device)
        {
            device->AddRef();
            device->GetImmediateContext(&deviceContext);
        }

        ~VideoInput()
        {
            SafeRelease(texture);
            SafeRelease(deviceContext);
            SafeRelease(device);
            SafeRelease(mediaBuffer);
        }

        void CopyFrom(ID3D11Texture2D* source)
        {
            if (texture == nullptr)
            {
                D3D11_TEXTURE2D_DESC existingDesc;
                source->GetDesc(&existingDesc);

                D3D11_TEXTURE2D_DESC textureDesc;
                ZeroMemory(&textureDesc, sizeof(textureDesc));
                textureDesc.Width = existingDesc.Width;
                textureDesc.Height = existingDesc.Height;
                textureDesc.MipLevels = existingDesc.MipLevels;
                textureDesc.ArraySize = existingDesc.ArraySize;
                textureDesc.Format = existingDesc.Format;
                textureDesc.SampleDesc.Count = existingDesc.SampleDesc.Count;
                textureDesc.SampleDesc.Quality = existingDesc.SampleDesc.Quality;
                textureDesc.Usage = D3D11_USAGE_DEFAULT;

                HRESULT hr = device->CreateTexture2D(&textureDesc, NULL, &texture);
                if (SUCCEEDED(hr)) MFCreateDXGISurfaceBuffer(IID_ID3D11Texture2D, texture, 0, true, &mediaBuffer);
                if (FAILED(hr))
                {
                    OutputDebugString(L"Creating video frame failed");
                }
            }

            deviceContext->CopyResource(texture, source);
        }
    };
#else
    class VideoInput : public VideoInputBase
    {
        byte* buffer = nullptr;
    public:
        VideoInput(size_t bufferSize)
        {
            auto hr = MFCreateMemoryBuffer(bufferSize, &mediaBuffer);
        }

        ~VideoInput()
        {
            Unlock();
            SafeRelease(mediaBuffer);
        }

        byte* Lock()
        {
            if (buffer == nullptr)
            {
                mediaBuffer->Lock(&buffer, NULL, NULL);
            }
            return buffer;
        }

        void Unlock()
        {
            if (buffer != nullptr)
            {
                mediaBuffer->Unlock();
                buffer = nullptr;
            }
        }
    };
#endif

private:
    void WriteVideo(std::unique_ptr<VideoInput> frame);
    void WriteAudio(byte* buffer, int bufferSize, LONGLONG timestamp);

    LARGE_INTEGER freq;

    class AudioInput
    {
    public:
        byte* buffer;
        LONGLONG timestamp;
        int bufferSize;

        AudioInput(byte* buffer, int buffSize, LONGLONG timestamp)
        {
            bufferSize = buffSize;
            this->buffer = new byte[buffSize];
            memcpy(this->buffer, buffer, buffSize);
            this->timestamp = timestamp;
        }
    };

    IMFSinkWriter* sinkWriter;
    DWORD videoStreamIndex = MAXDWORD;
    DWORD audioStreamIndex = MAXDWORD;

    bool isRecording = false;
    bool acceptQueuedFrames = false;

    // Video Parameters.
    UINT frameWidth;
    UINT frameHeight;
    UINT frameStride;
    UINT32 fps;
    UINT32 bitRate;
    UINT32 videoEncodingMpegLevel;
    GUID videoEncodingFormat;
    GUID inputFormat;
    LONGLONG prevVideoTime = INVALID_TIMESTAMP;

    // Audio Parameters.
    UINT32 audioSampleRate;
    UINT32 audioChannels;
    UINT32 audioBPS;
    LONGLONG prevAudioTime = INVALID_TIMESTAMP;

    LONGLONG startTime = INVALID_TIMESTAMP;

    std::queue<std::unique_ptr<VideoInput>> videoInputPool;
    std::queue<std::unique_ptr<VideoInput>> videoQueue;
    std::queue<AudioInput> audioQueue;

    std::shared_mutex videoStateLock;
    std::shared_mutex videoInputPoolLock;
    std::future<void> videoWriteFuture;

#if HARDWARE_ENCODE_VIDEO
    IMFDXGIDeviceManager* deviceManager = NULL;
    UINT resetToken = 0;
#endif
};

