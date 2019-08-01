// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once

// Performance flags, set these to FALSE to improve performance for lower end hardware.
// Note setting these to false may reduce image quality or hologram stability.

//TODO: Set this to true if using a USB 3 external BlackMagic Shuttle capture card.
#define USE_DECKLINK_SHUTTLE    FALSE

// Audio
//TODO: Set this to true to encode audio with captured video.
//NOTE: If you do not have Audio data, set this to false or the video may encode incorrectly.
#define ENCODE_AUDIO TRUE

// These should match Game Engine's audio settings.  Size is in bytes.
// These values should also be valid data values for H.264 encoding:
// https://msdn.microsoft.com/en-us/library/windows/desktop/dd742785(v=vs.85).aspx
// NOTE: Audio bits per sample must be 16.
// This should match size (in bytes) of audio data from Engine.
// Note: Audio from Engine must be scaled to short.MinValue..short.MaxValue and converted to a byte array.
// In Unity, audio is retrieved as a float array -1..1, but the channel size is:
// sizeof(short) * (arrayLength / numChannels) instead of sizeof(float)
// since we need to convert the floats to shorts for audio encoding.
#define AUDIO_CHANNEL_SIZE  2048
// This must be 1, 2, or 6 (if Win10)
#define AUDIO_CHANNELS      2
// This must be 44100 or 48000
#define AUDIO_SAMPLE_RATE   48000
// Output audio resolution - Average bytes per second.
// This must be: 12000, 16000, 20000, or 24000.
#define AUDIO_BPS           24000

/* With a resolution of 1080p, 60 FPS and a desired bitrate of 62,5 MBit/s after compression the following level-profile combination is needed:
 * - Level   = 4.2 (allows for max. 1920x1080 @ 64 FPS)
 * - Profile = High (allows for max. 62.5 MBit/s)With a resolution of 4K, 60 FPS and a desired bitrate of 300 MBit/s after compression the following level-profile combination is needed:
 * With a resolution of 4K, 60 FPS and a desired bitrate of 300 MBit/s after compression the following level-profile combination is needed:
 * - Level   = 5.2 (allows for max. 3840x2160 @ 66.8 FPS)
 * - Profile = High (allows for max. 300 MBit/s)
 *
 * See: https://de.wikipedia.org/wiki/H.264#Level
 */
#define VIDEO_BITRATE_1080P         (62 * 1000 * 1000 + 500 * 1000) // 62.5 Mbit/s
#define VIDEO_BITRATE_4K            (300 * 1000 * 1000)             // 300 MBit/s
#define VIDEO_MPEG_LEVEL_1080P      eAVEncH264VLevel4_2
#define VIDEO_MPEG_LEVEL_4K         eAVEncH264VLevel5_2

// Frame Dimensions and buffer lengths
//TODO: change this to match video dimensions from your tethered camera.
#define FRAME_WIDTH    1920
#define FRAME_HEIGHT   1080

#define QUAD_FRAME_WIDTH (FRAME_WIDTH * 2)
#define QUAD_FRAME_HEIGHT (FRAME_HEIGHT * 2)

#define FRAME_BPP_RGBA 4
#define FRAME_BPP_YUV  2
#define FRAME_BPP_NV12 1.5f

// Color camera buffer size.
#define FRAME_BUFSIZE_RGBA          (FRAME_WIDTH * FRAME_HEIGHT * FRAME_BPP_RGBA)
#define FRAME_BUFSIZE_YUV           (FRAME_WIDTH * FRAME_HEIGHT * FRAME_BPP_YUV)
#define FRAME_BUFSIZE_NV12          ((int)(FRAME_WIDTH * FRAME_HEIGHT * FRAME_BPP_NV12))
#define QUAD_FRAME_BUFSIZE_RGBA     (FRAME_BUFSIZE_RGBA * 4)
#define QUAD_FRAME_BUFSIZE_YUV      (FRAME_BUFSIZE_YUV * 4)
#define QUAD_FRAME_BUFSIZE_NV12     (FRAME_BUFSIZE_NV12 * 4)

// Return timestamps in HNS.  Do not change this value.
#define QPC_MULTIPLIER 10000000

#define VIDEO_FPS 30

// Default number of frames to offset Color timestamp to account for latency between
// capturing a frame on the camera and retrieving from the capture card.
#define INITIAL_FRAME_OFFSET 0.0f

#define HARDWARE_ENCODE_VIDEO TRUE
