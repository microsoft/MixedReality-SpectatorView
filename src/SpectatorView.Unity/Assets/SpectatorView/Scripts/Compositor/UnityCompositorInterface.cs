// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Microsoft.MixedReality.SpectatorView
{
    internal static class UnityCompositorInterface
    {
        private const string CompositorPluginDll = "SpectatorView.Compositor.UnityPlugin";

        [DllImport(CompositorPluginDll)]
        public static extern int GetFrameWidth();

        [DllImport(CompositorPluginDll)]
        public static extern int GetFrameHeight();

        [DllImport(CompositorPluginDll)]
        public static extern bool SetHoloTexture(IntPtr holoTexture);

        [DllImport(CompositorPluginDll)]
        public static extern void SetAlpha(float alpha);

        [DllImport(CompositorPluginDll)]
        public static extern float GetAlpha();

        [DllImport(CompositorPluginDll)]
        public static extern bool CreateUnityColorTexture(out IntPtr srv);

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

        [DllImport(CompositorPluginDll)]
        public static extern void StartRecording();

        [DllImport(CompositorPluginDll)]
        public static extern void StopRecording();
        
        [DllImport(CompositorPluginDll)]
        public static extern bool InitializeFrameProviderOnDevice(int providerId);  //0 = blackmagic, 1 = elgato

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
        public static extern void UpdateSpectatorView();

        [DllImport(CompositorPluginDll)]
        public static extern int GetCaptureFrameIndex();

        [DllImport(CompositorPluginDll)]
        public static extern void SetCompositeFrameIndex(int index);
    }
}
#endif
