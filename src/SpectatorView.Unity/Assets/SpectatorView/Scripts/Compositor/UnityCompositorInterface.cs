// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public enum FrameProviderDeviceType : int { BlackMagic = 0, Elgato = 1, AzureKinect = 2, None = 3 };
    public enum VideoRecordingFrameLayout : int { Composite = 0, Quad = 1 };

#if UNITY_EDITOR
    internal struct CompositorVector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3 AsPosition()
        {
            return new Vector3(x, -y, z);
        }

        public Vector3 AsRodriguesRotation()
        {
            return new Vector3(x, y, z);
        }
    }

    internal struct CompositorCameraIntrinsics
    {
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public int imageWidth;
        public int imageHeight;

        public CalculatedCameraIntrinsics AsCalculatedCameraIntrinsics()
        {
            return new CalculatedCameraIntrinsics(
                reprojectionError: 0.0f,
                focalLength: new Vector2(fx, fy),
                imageWidth: (uint)imageWidth,
                imageHeight: (uint)imageHeight,
                principalPoint: new Vector2(cx, cy),
                radialDistortion: Vector3.zero,
                tangentialDistortion: Vector2.zero,
                undistortedProjectionTransform: Matrix4x4.identity
            );
        }
    }

    internal static class UnityCompositorInterface
    {
        private const string CompositorPluginDll = "SpectatorView.Compositor.UnityPlugin";

        [DllImport(CompositorPluginDll)]
        public static extern int GetFrameWidth();

        [DllImport(CompositorPluginDll)]
        public static extern int GetFrameHeight();

        [DllImport(CompositorPluginDll)]
        public static extern int GetVideoRecordingFrameWidth([MarshalAs(UnmanagedType.I4)] VideoRecordingFrameLayout frameLayout);

        [DllImport(CompositorPluginDll)]
        public static extern int GetVideoRecordingFrameHeight([MarshalAs(UnmanagedType.I4)] VideoRecordingFrameLayout frameLayout);

        [DllImport(CompositorPluginDll)]
        public static extern bool SetHoloTexture(IntPtr holoTexture);

        [DllImport(CompositorPluginDll)]
        public static extern void SetAlpha(float alpha);

        [DllImport(CompositorPluginDll)]
        public static extern float GetAlpha();

        [DllImport(CompositorPluginDll)]
        public static extern bool CreateUnityColorTexture(out IntPtr srv);

        [DllImport(CompositorPluginDll)]
        public static extern bool CreateUnityDepthCameraTexture(out IntPtr srv);

        [DllImport(CompositorPluginDll)]
        public static extern bool CreateUnityHoloTexture(out IntPtr srv);

        [DllImport(CompositorPluginDll)]
        public static extern bool SetMergedRenderTexture(IntPtr texturePtr);

        [DllImport(CompositorPluginDll)]
        public static extern bool SetVideoRenderTexture(IntPtr texturePtr);

        [DllImport(CompositorPluginDll)]
        public static extern bool SetOutputRenderTexture(IntPtr texturePtr);

        [DllImport(CompositorPluginDll)]
        public static extern bool IsRecording();

        [DllImport(CompositorPluginDll)]
        public static extern bool OutputYUV();

        [DllImport(CompositorPluginDll)]
        public static extern bool QueueingHoloFrames();

        [DllImport(CompositorPluginDll)]
        public static extern bool HardwareEncodeVideo();

        [DllImport(CompositorPluginDll)]
        public static extern void StopFrameProvider();

        [DllImport(CompositorPluginDll)]
        public static extern void TakePicture();

        [DllImport(CompositorPluginDll, CharSet = CharSet.Unicode)]
        public static extern void TakeRawPicture(string path);

        [DllImport(CompositorPluginDll, CharSet = CharSet.Unicode)]
        public static extern bool StartRecording(int frameLayout, string desiredFileName, int desiredFileNameLength, int inputFileNameLength, StringBuilder fileName, int[] fileNameLength);

        [DllImport(CompositorPluginDll)]
        public static extern void StopRecording();

        [DllImport(CompositorPluginDll)]
        public static extern bool IsFrameProviderSupported([MarshalAs(UnmanagedType.I4)] FrameProviderDeviceType providerId);

        [DllImport(CompositorPluginDll)]
        public static extern bool InitializeFrameProviderOnDevice([MarshalAs(UnmanagedType.I4)] FrameProviderDeviceType providerId);

        [DllImport(CompositorPluginDll)]
        public static extern void Reset();

        [DllImport(CompositorPluginDll)]
        public static extern long GetColorDuration();

        [DllImport(CompositorPluginDll)]
        public static extern int GetNumQueuedOutputFrames();

        [DllImport(CompositorPluginDll)]
        public static extern IntPtr GetRenderEventFunc();

        [DllImport(CompositorPluginDll)]
        public static extern void SetAudioData(byte[] audioData, int dataLength, double audioTime);

        [DllImport(CompositorPluginDll)]
        public static extern void UpdateCompositor();

        [DllImport(CompositorPluginDll)]
        public static extern int GetCaptureFrameIndex();

        [DllImport(CompositorPluginDll)]
        public static extern void SetCompositeFrameIndex(int index);

        [DllImport(CompositorPluginDll)]
        public static extern bool IsCameraCalibrationInformationAvailable();

        [DllImport(CompositorPluginDll)]
        public static extern void GetCameraCalibrationInformation(out CompositorCameraIntrinsics cameraIntrinsics);

        [DllImport(CompositorPluginDll)]
        public static extern void ConfigureArUcoMarkerDetector(float markerSize);

        [DllImport(CompositorPluginDll)]
        public static extern bool TryGetLatestArUcoMarkerPose(int markerId, out CompositorVector3 position, out CompositorVector3 rotation);
    }
#endif
}
