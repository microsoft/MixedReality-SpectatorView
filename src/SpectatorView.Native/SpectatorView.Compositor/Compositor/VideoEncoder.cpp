// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "pch.h"
#include "VideoEncoder.h"

#include "codecapi.h"

#define NUM_VIDEO_BUFFERS 10

VideoEncoder::VideoEncoder(UINT frameWidth, UINT frameHeight, UINT frameStride, UINT fps,
    UINT32 audioSampleRate, UINT32 audioChannels, UINT32 audioBPS, UINT32 videoBitrate, UINT32 videoMpegLevel) :
    frameWidth(frameWidth),
    frameHeight(frameHeight),
    frameStride(frameStride),
    audioSampleRate(audioSampleRate),
    audioChannels(audioChannels),
    audioBPS(audioBPS),
    fps(fps),
    bitRate(videoBitrate),
    videoEncodingFormat(MFVideoFormat_H264),
    videoEncodingMpegLevel(videoMpegLevel),
    isRecording(false)
{
#if HARDWARE_ENCODE_VIDEO
    inputFormat = MFVideoFormat_NV12;
#else
    inputFormat = MFVideoFormat_RGB32;
#endif

    for (int i = 0; i < NUM_VIDEO_BUFFERS; i++)
    {
        videoInputPool.push(std::make_unique<VideoInput>(frameHeight * frameStride));
    }
}

VideoEncoder::~VideoEncoder()
{
    MFShutdown();
}

bool VideoEncoder::Initialize(ID3D11Device* device)
{
    HRESULT hr = E_PENDING;
    hr = MFStartup(MF_VERSION);

    QueryPerformanceFrequency(&freq);

#if HARDWARE_ENCODE_VIDEO
    MFCreateDXGIDeviceManager(&resetToken, &deviceManager);

    if (deviceManager != nullptr)
    {
        OutputDebugString(L"Resetting device manager with graphics device.\n");
        deviceManager->ResetDevice(device, resetToken);
    }
#endif

    return SUCCEEDED(hr);
}

bool VideoEncoder::IsRecording()
{
    return isRecording;
}

void VideoEncoder::StartRecording(LPCWSTR videoPath, bool encodeAudio)
{
    std::unique_lock<std::shared_mutex> lock(videoStateLock);

    if (isRecording)
    {
		OutputDebugString(L"StartRecording called when device was already recording.\n");
        return;
    }

    // Reset previous times to get valid data for this recording.
	startTime = INVALID_TIMESTAMP;
    prevVideoTime = INVALID_TIMESTAMP;
    prevAudioTime = INVALID_TIMESTAMP;

    HRESULT hr = E_PENDING;

    sinkWriter = NULL;
    videoStreamIndex = MAXDWORD;
    audioStreamIndex = MAXDWORD;

    IMFMediaType*    pVideoTypeOut = NULL;
    IMFMediaType*    pVideoTypeIn = NULL;

#if ENCODE_AUDIO
    IMFMediaType*    pAudioTypeOut = NULL;
    IMFMediaType*    pAudioTypeIn = NULL;
#endif

    IMFAttributes *attr = nullptr;
    MFCreateAttributes(&attr, 3);

    if (SUCCEEDED(hr)) { hr = attr->SetUINT32(MF_SINK_WRITER_DISABLE_THROTTLING, TRUE); }

#if HARDWARE_ENCODE_VIDEO
    if (SUCCEEDED(hr)) { hr = attr->SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, true); }
    if (SUCCEEDED(hr)) { hr = attr->SetUINT32(MF_READWRITE_DISABLE_CONVERTERS, false); }
#endif

    hr = MFCreateSinkWriterFromURL(videoPath, NULL, attr, &sinkWriter);

    // Set the output media types.
    if (SUCCEEDED(hr)) { hr = MFCreateMediaType(&pVideoTypeOut); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetGUID(MF_MT_SUBTYPE, videoEncodingFormat); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetUINT32(MF_MT_AVG_BITRATE, bitRate); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeSize(pVideoTypeOut, MF_MT_FRAME_SIZE, frameWidth, frameHeight); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeRatio(pVideoTypeOut, MF_MT_FRAME_RATE, fps, 1); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeRatio(pVideoTypeOut, MF_MT_PIXEL_ASPECT_RATIO, 1, 1); }

    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetUINT32(MF_MT_MPEG2_LEVEL, videoEncodingMpegLevel); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeOut->SetUINT32(MF_MT_MPEG2_PROFILE, eAVEncH264VProfile_High); }

    if (SUCCEEDED(hr)) { hr = sinkWriter->AddStream(pVideoTypeOut, &videoStreamIndex); }

    if (encodeAudio)
    {
#if ENCODE_AUDIO
        if (SUCCEEDED(hr)) { hr = MFCreateMediaType(&pAudioTypeOut); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_AAC); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, audioSampleRate); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, audioChannels); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, audioBPS); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_AUDIO_PREFER_WAVEFORMATEX, 1); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_ALL_SAMPLES_INDEPENDENT, 1); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeOut->SetUINT32(MF_MT_FIXED_SIZE_SAMPLES, 1); }
        if (SUCCEEDED(hr)) { hr = sinkWriter->AddStream(pAudioTypeOut, &audioStreamIndex); }
#endif
    }

    // Set the input media types.
    if (SUCCEEDED(hr)) { hr = MFCreateMediaType(&pVideoTypeIn); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeIn->SetGUID(MF_MT_SUBTYPE, inputFormat); }
    if (SUCCEEDED(hr)) { hr = pVideoTypeIn->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeSize(pVideoTypeIn, MF_MT_FRAME_SIZE, frameWidth, frameHeight); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeRatio(pVideoTypeIn, MF_MT_FRAME_RATE, fps, 1); }
    if (SUCCEEDED(hr)) { hr = MFSetAttributeRatio(pVideoTypeIn, MF_MT_PIXEL_ASPECT_RATIO, 1, 1); }
    if (SUCCEEDED(hr)) { hr = sinkWriter->SetInputMediaType(videoStreamIndex, pVideoTypeIn, NULL); }

    if (encodeAudio)
    {
#if ENCODE_AUDIO
        if (SUCCEEDED(hr)) { hr = MFCreateMediaType(&pAudioTypeIn); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeIn->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeIn->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeIn->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, audioSampleRate); }
        if (SUCCEEDED(hr)) { hr = pAudioTypeIn->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, audioChannels); }
        if (SUCCEEDED(hr)) { hr = sinkWriter->SetInputMediaType(audioStreamIndex, pAudioTypeIn, NULL); }
#endif
    }

    // Tell the sink writer to start accepting data.
    if (SUCCEEDED(hr)) { hr = sinkWriter->BeginWriting(); }

    if (FAILED(hr))
    {
        OutputDebugString(L"Error starting recording.\n");
    }

    isRecording = true;
    acceptQueuedFrames = true;

    SafeRelease(pVideoTypeOut);
    SafeRelease(pVideoTypeIn);

#if ENCODE_AUDIO
    SafeRelease(pAudioTypeOut);
    SafeRelease(pAudioTypeIn);
#endif
}

void VideoEncoder::WriteAudio(byte* buffer, int bufferSize, LONGLONG timestamp)
{
    std::shared_lock<std::shared_mutex> lock(videoStateLock);
#if _DEBUG
	{
		std::wstring debugString = L"Writing Audio, Timestamp:" + std::to_wstring(timestamp) + L"\n";
		OutputDebugString(debugString.data());
	}
#endif

#if ENCODE_AUDIO
    if (!isRecording)
    {
		std::wstring debugString = L"WriteAudio call failed: StartTime:" + std::to_wstring(startTime) + L", Timestamp:" + std::to_wstring(timestamp) + L"\n";
		OutputDebugString(debugString.data());
        return;
    }
	else if (startTime == INVALID_TIMESTAMP)
	{
		startTime = timestamp;
#if _DEBUG 
		std::wstring debugString = L"Start time set from audio, Timestamp:" + std::to_wstring(timestamp) + L", StartTime:" + std::to_wstring(startTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
	}
	else if (timestamp < startTime)
	{
#if _DEBUG 
		std::wstring debugString = L"Audio not recorded, Timestamp less than start time. Timestamp:" + std::to_wstring(timestamp) + L", StartTime:" + std::to_wstring(startTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
		return;
	}

    LONGLONG sampleTimeNow = timestamp;
    LONGLONG sampleTimeStart = startTime;

    LONGLONG sampleTime = sampleTimeNow - sampleTimeStart;

    LONGLONG duration = ((LONGLONG)((((float)AUDIO_SAMPLE_RATE * (16.0f /*bits per sample*/ / 8.0f /*bits per byte*/)) / (float)bufferSize) * 10000));
    if (prevAudioTime != INVALID_TIMESTAMP)
    {
        duration = sampleTime - prevAudioTime;
#if _DEBUG 
		std::wstring debugString = L"Updated write audio duration:" + std::to_wstring(duration) + L", SampleTime:" + std::to_wstring(sampleTime) + L", PrevAudioTime:" + std::to_wstring(prevAudioTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
    }

    // Copy frame to a temporary buffer and process on a background thread.
    byte* tmpAudioBuffer = new byte[bufferSize];
    memcpy(tmpAudioBuffer, buffer, bufferSize);

    concurrency::create_task([=]()
    {
        std::shared_lock<std::shared_mutex> lock(videoStateLock);

        HRESULT hr = E_PENDING;
        if (sinkWriter == NULL || !isRecording)
        {
            OutputDebugString(L"Must start recording before writing audio frames.\n");
            delete[] tmpAudioBuffer;
            return;
        }

        IMFSample* pAudioSample = NULL;
        IMFMediaBuffer* pAudioBuffer = NULL;

        const DWORD cbAudioBuffer = bufferSize;

        BYTE* pData = NULL;

        hr = MFCreateMemoryBuffer(cbAudioBuffer, &pAudioBuffer);
        if (SUCCEEDED(hr)) { hr = pAudioBuffer->Lock(&pData, NULL, NULL); }
        memcpy(pData, tmpAudioBuffer, cbAudioBuffer);
        if (pAudioBuffer)
        {
            pAudioBuffer->Unlock();
        }


#if _DEBUG
		{
			std::wstring debugString = L"Writing Audio Sample, SampleTime:" + std::to_wstring(sampleTime) + L", SampleDuration:" + std::to_wstring(duration) + L", BufferLength:" + std::to_wstring(cbAudioBuffer) + L"\n";
			OutputDebugString(debugString.data());
		}
#endif

        if (SUCCEEDED(hr)) { hr = MFCreateSample(&pAudioSample); }
        if (SUCCEEDED(hr)) { hr = pAudioSample->SetSampleTime(sampleTime); }
        if (SUCCEEDED(hr)) { hr = pAudioSample->SetSampleDuration(duration); }
        if (SUCCEEDED(hr)) { hr = pAudioBuffer->SetCurrentLength(cbAudioBuffer); }
        if (SUCCEEDED(hr)) { hr = pAudioSample->AddBuffer(pAudioBuffer); }

        if (SUCCEEDED(hr)) { hr = sinkWriter->WriteSample(audioStreamIndex, pAudioSample); }

        SafeRelease(pAudioSample);
        SafeRelease(pAudioBuffer);

        if (FAILED(hr))
        {
            OutputDebugString(L"Error writing audio frame.\n");
        }

        delete[] tmpAudioBuffer;
    });

    prevAudioTime = sampleTime;
#endif
}

void VideoEncoder::WriteVideo(std::unique_ptr<VideoEncoder::VideoInput> frame)
{
    std::shared_lock<std::shared_mutex> lock(videoStateLock);
#if _DEBUG
	{
		std::wstring debugString = L"Writing Video, Timestamp:" + std::to_wstring(frame->timestamp) + L"\n";
		OutputDebugString(debugString.data());
	}
#endif

    if (!isRecording)
    {
		OutputDebugString(L"Video not recorded, encoder not currently recording");
        return;
    }

	if (startTime == INVALID_TIMESTAMP)
	{
		startTime = frame->timestamp;
#if _DEBUG 
		std::wstring debugString = L"Start time set from video, Timestamp:" + std::to_wstring(frame->timestamp) + L", StartTime:" + std::to_wstring(startTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
	}
    else if (frame->timestamp < startTime)
    {
#if _DEBUG 
		std::wstring debugString = L"Video not recorded, Timestamp less than start time. Timestamp:" + std::to_wstring(frame->timestamp) + L", StartTime:" + std::to_wstring(startTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
        return;
    }

    if (frame->timestamp == prevVideoTime)
    {
#if _DEBUG 
		std::wstring debugString = L"Video not recorded, Timestamp equals prevVideoTime. Timestamp:" + std::to_wstring(frame->timestamp) + L", StartTime:" + std::to_wstring(prevVideoTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
        return;
    }
    
    LONGLONG sampleTimeNow = frame->timestamp;
    LONGLONG sampleTimeStart = startTime;

    LONGLONG sampleTime = sampleTimeNow - sampleTimeStart;

    if (prevVideoTime != INVALID_TIMESTAMP)
    {
        frame->duration = sampleTime - prevVideoTime;
#if _DEBUG 
		std::wstring debugString = L"Updated write video duration:" + std::to_wstring(frame->duration) + L", SampleTime:" + std::to_wstring(sampleTime) + L", PrevVideoTime:" + std::to_wstring(prevVideoTime) + L"\n";
		OutputDebugString(debugString.data());
#endif
    }

    videoWriteFuture = std::async(std::launch::async, [=, frame{ std::move(frame) }, previousWriteFuture{ std::move(videoWriteFuture) }]() mutable
    {
        if (previousWriteFuture.valid())
        {
            previousWriteFuture.wait();
        }
        std::shared_lock<std::shared_mutex> lock(videoStateLock);

        HRESULT hr = E_PENDING;
        if (sinkWriter == NULL || !isRecording)
        {
            OutputDebugString(L"Must start recording before writing video frames.\n");
            return;
        }

        LONG cbWidth = frameStride;
        DWORD cbBuffer = cbWidth * frameHeight;
        DWORD imageHeight = frameHeight;

#if HARDWARE_ENCODE_VIDEO
        cbWidth = frameWidth;
        cbBuffer = (int)(FRAME_BPP_NV12 * frameWidth * frameHeight);
        imageHeight = (int)(FRAME_BPP_NV12 * frameHeight);
#endif

        IMFSample* pVideoSample = NULL;
        IMFMediaBuffer* pVideoBuffer = NULL;
        BYTE* pData = NULL;

        // Create a new memory buffer.
        hr = MFCreateMemoryBuffer(cbBuffer, &pVideoBuffer);

        // Lock the buffer and copy the video frame to the buffer.
        if (SUCCEEDED(hr)) { hr = pVideoBuffer->Lock(&pData, NULL, NULL); }

        if (SUCCEEDED(hr))
        {
            //TODO: Can pVideoBuffer be created from an ID3D11Texture2D*?
            hr = MFCopyImage(
                pData,                      // Destination buffer.
                cbWidth,                    // Destination stride.
                frame->buffer,
                cbWidth,                    // Source stride.
                cbWidth,                    // Image width in bytes.
                imageHeight                 // Image height in pixels.
            );
        }

        if (pVideoBuffer)
        {
            pVideoBuffer->Unlock();
        }

#if _DEBUG
		{
			std::wstring debugString = L"Writing Video Sample, SampleTime:" + std::to_wstring(sampleTime) + L", SampleDuration:" + std::to_wstring(frame->duration) + L", BufferLength:" + std::to_wstring(cbBuffer) + L"\n";
			OutputDebugString(debugString.data());
		}
#endif

        // Set the data length of the buffer.
        if (SUCCEEDED(hr)) { hr = pVideoBuffer->SetCurrentLength(cbBuffer); }

        // Create a media sample and add the buffer to the sample.
        if (SUCCEEDED(hr)) { hr = MFCreateSample(&pVideoSample); }
        if (SUCCEEDED(hr)) { hr = pVideoSample->AddBuffer(pVideoBuffer); }

        if (SUCCEEDED(hr)) { hr = pVideoSample->SetSampleTime(sampleTime); } //100-nanosecond units
        if (SUCCEEDED(hr)) { hr = pVideoSample->SetSampleDuration(frame->duration); } //100-nanosecond units

        // Send the sample to the Sink Writer.
        if (SUCCEEDED(hr)) { hr = sinkWriter->WriteSample(videoStreamIndex, pVideoSample); }

        SafeRelease(pVideoSample);
        SafeRelease(pVideoBuffer);

        {
            std::shared_lock<std::shared_mutex>(videoInputPoolLock);
            videoInputPool.push(std::move(frame));
        }

        if (FAILED(hr))
        {
            OutputDebugString(L"Error writing video frame.\n");
        }
    });

    prevVideoTime = sampleTime;
}

void VideoEncoder::StopRecording()
{
    std::unique_lock<std::shared_mutex> lock(videoStateLock);

    if (sinkWriter == NULL || !isRecording)
    {
        OutputDebugString(L"Must start recording before it can be stopped.\n");
        return;
    }

    // Clear any async frames.
    acceptQueuedFrames = false;
    isRecording = false;

    std::mutex completion_mutex;

    bool doneCleaningVideoTasks = false;
    bool doneCleaningAudioTasks = false;

    std::unique_lock<std::mutex> completion_lock(completion_mutex);
    std::condition_variable completion_lock_check;

    concurrency::create_task([&]
    {
        while (!videoQueue.empty())
        {
			videoQueue.pop();
        }
#if _DEBUG
		OutputDebugString(L"Cleared video queue\n");
#endif

        {
            std::lock_guard<std::mutex> lk(completion_mutex);
            doneCleaningVideoTasks = true;
        }
        completion_lock_check.notify_one();
    });

    concurrency::create_task([&]
    {
        while (!audioQueue.empty())
        {
            audioQueue.pop();
        }
#if _DEBUG
		OutputDebugString(L"Cleared audio queue\n");
#endif

        {
            std::lock_guard<std::mutex> lk(completion_mutex);
            doneCleaningAudioTasks = true;
        }
        completion_lock_check.notify_one();
    });

    completion_lock_check.wait(completion_lock, [&] {return doneCleaningVideoTasks && doneCleaningAudioTasks; });
	OutputDebugString(L"Completed clearing audio/video queues\n");

    if (videoStreamIndex != MAXDWORD)
    {
		OutputDebugString(L"Flushing video stream\n");
        sinkWriter->Flush(videoStreamIndex);
		videoStreamIndex = MAXDWORD;
    }
    if (audioStreamIndex != MAXDWORD)
    {
		OutputDebugString(L"Flushing audio stream\n");
        sinkWriter->Flush(audioStreamIndex);
		audioStreamIndex = MAXDWORD;
    }

    sinkWriter->Finalize();
    SafeRelease(sinkWriter);
}

std::unique_ptr<VideoEncoder::VideoInput> VideoEncoder::GetAvailableVideoFrame()
{
    std::shared_lock<std::shared_mutex> lock(videoInputPoolLock);
    if (videoInputPool.empty())
    {
        return std::make_unique<VideoInput>(frameStride * frameHeight);
    }
    else
    {
        auto result = std::move(videoInputPool.front());
        videoInputPool.pop();
        return result;
    }
}

void VideoEncoder::QueueVideoFrame(std::unique_ptr<VideoEncoder::VideoInput> frame)
{
    std::shared_lock<std::shared_mutex> lock(videoStateLock);

    if (acceptQueuedFrames)
    {
#if _DEBUG
        std::wstring debugString = L"Pushed Video Input, Timestamp:" + std::to_wstring(frame->timestamp) + L"\n";
        OutputDebugString(debugString.data());
#endif
        videoQueue.push(std::move(frame));
    }
}

void VideoEncoder::QueueAudioFrame(byte* buffer, int bufferSize, LONGLONG timestamp)
{
    std::shared_lock<std::shared_mutex> lock(videoStateLock);

    if (acceptQueuedFrames)
    {
        audioQueue.push(AudioInput(buffer, bufferSize, timestamp));
#if _DEBUG
		std::wstring debugString = L"Pushed Audio Input, Timestamp:" + std::to_wstring(timestamp) + L"\n";
		OutputDebugString(debugString.data());
#endif
    }
}

void VideoEncoder::Update()
{
    std::shared_lock<std::shared_mutex> lock(videoStateLock);
    if (!isRecording)
    {
        return;
    }

    while (!videoQueue.empty())
    {
        if (isRecording)
        {
            WriteVideo(std::move(videoQueue.front()));
            videoQueue.pop();
        }
    }

    while (!audioQueue.empty())
    {
        if (isRecording)
        {
            AudioInput input = audioQueue.front();
            WriteAudio(input.buffer, input.bufferSize, input.timestamp);
            delete[] input.buffer;
            audioQueue.pop();
        }
    }
}
