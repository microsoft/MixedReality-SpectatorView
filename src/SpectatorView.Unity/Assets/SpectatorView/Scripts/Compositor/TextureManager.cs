// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#define OUTPUT_YUV

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Rendering;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Manages the textures used for compositing holograms with video, and controls
    /// the actual composition of textures together.
    /// </summary>
    public class TextureManager : MonoBehaviour
    {
#if UNITY_EDITOR
        public CompositionManager Compositor { get; set; }

        /// <summary>
        /// Gets or sets whether or not the quadrant video output is required.
        /// The quadrant frame will not be rendered when this is not set and when
        /// the video recording mode does not require it.
        /// </summary>
        public bool IsQuadrantVideoOutputRequired { get; set; }

        /// <summary>
        /// The color image texture coming from the camera, converted to RGB. The Unity camera is "cleared" to this texture
        /// </summary>
        public RenderTexture colorRGBTexture { get; private set; }

        /// <summary>
        /// The final step of rendering holograms (on top of color texture)
        /// </summary>
        public RenderTexture renderTexture { get; private set; }

        /// <summary>
        /// The final composite texture (hologram opacity reduced based on alpha setting)
        /// </summary>
        public RenderTexture compositeTexture { get; private set; }

        /// <summary>
        /// An RGBA texture where all 4 channels contain the hologram alpha value
        /// </summary>
        public RenderTexture alphaTexture { get; private set; }

        /// <summary>
        /// The texture containing the raw video, alpha mask, hologram, and composite textures.
        /// </summary>
        public RenderTexture quadViewOutputTexture { get; private set; }

        /// <summary>
        /// The raw color image data coming from the capture card
        /// </summary>
        private Texture2D colorTexture = null;

        /// <summary>
        /// An override texture for testing calibration
        /// </summary>
        private Texture2D overrideColorTexture = null;

        /// <summary>
        /// The final composite texture converted to NV12 format for use in creating video file
        /// </summary>
        private RenderTexture videoOutputTexture = null;

        /// <summary>
        /// The final composite texture converted into the format expected by output on the capture card (YUV or BGRA)
        /// </summary>
        private RenderTexture displayOutputTexture = null;

        /// <summary>
        /// Gets whether or not holograms should be rendered on a black background (which allows for post-processing in quadrant mode)
        /// or against the video background (which allows for correct alpha blending for partially-transparent holograms).
        /// </summary>
        private bool IsVideoOutputQuadrantMode => Compositor != null && Compositor.VideoRecordingLayout == VideoRecordingFrameLayout.Quad;

        private bool ShouldProduceQuadrantVideoFrame => IsQuadrantVideoOutputRequired || IsVideoOutputQuadrantMode;

        public RenderTexture[] supersampleBuffers;

        public event Action TextureRenderCompleted;

        private Material ignoreAlphaMat;
        private Material BGRToRGBMat;
        private Material RGBToBGRMat;
        private Material YUVToRGBMat;
        private Material RGBToYUVMat;
        private Material NV12VideoMat;
        private Material BGRVideoMat;
        private Material holoAlphaMat;
        private Material quadViewMat;
        private Material alphaBlendMat;
        private Material textureClearMat;
        private Material extractAlphaMat;
        private Material downsampleMat;
        private Material[] downsampleMats;

        private Camera spectatorViewCamera;

        private int frameWidth;
        private int frameHeight;
        private bool outputYUV;
        private bool hardwareEncodeVideo;
        private IntPtr renderEvent;

        public Material IgnoreAlphaMaterial => ignoreAlphaMat;

        private Texture2D CurrentColorTexture
        {
            get
            {
                if (overrideColorTexture != null)
                {
                    return overrideColorTexture;
                }
                else
                {
                    return colorTexture;
                }
            }
        }

        private Material CurrentColorMaterial
        {
            get
            {
                if (overrideColorTexture == null && outputYUV)
                {
                    return YUVToRGBMat;
                }
                else
                {
                    return BGRToRGBMat;
                }
            }
        }

        /// <summary>
        /// Loads a material from Unity resources with the given name.
        /// </summary>
        /// <param name="materialName">The name of the material to load.</param>
        /// <returns>The material loaded from resources.</returns>
        private static Material LoadMaterial(string materialName)
        {
            Material material = new Material(Resources.Load<Material>("Materials/" + materialName));
            if (material == null)
            {
                Debug.LogError(materialName + " could not be found");
            }
            return material;
        }

        /// <summary>
        /// Sets a texture to use as a replacement for the video background texture.
        /// </summary>
        /// <param name="texture">A texture that overrides the video texture, or null to resume using the
        /// incoming video texture from the capture card.</param>
        public void SetOverrideColorTexture(Texture2D texture)
        {
            overrideColorTexture = texture;
            SetShaderValues();
        }

        private void Start()
        {
            frameWidth = UnityCompositorInterface.GetFrameWidth();
            frameHeight = UnityCompositorInterface.GetFrameHeight();
            outputYUV = UnityCompositorInterface.OutputYUV();
            renderEvent = UnityCompositorInterface.GetRenderEventFunc();
            hardwareEncodeVideo = UnityCompositorInterface.HardwareEncodeVideo();

            downsampleMat = LoadMaterial("Downsample");
            YUVToRGBMat = LoadMaterial("YUVToRGB");
            RGBToYUVMat = LoadMaterial("RGBToYUV");
            BGRToRGBMat = LoadMaterial("BGRToRGB");
            RGBToBGRMat = LoadMaterial("BGRToRGB");
            NV12VideoMat = LoadMaterial("RGBToNV12");
            BGRVideoMat = LoadMaterial("BGRToRGB");
            holoAlphaMat = LoadMaterial("HoloAlpha");
            extractAlphaMat = LoadMaterial("ExtractAlpha");
            ignoreAlphaMat = LoadMaterial("IgnoreAlpha");
            quadViewMat = LoadMaterial("QuadView");
            alphaBlendMat = LoadMaterial("AlphaBlend");
            textureClearMat = LoadMaterial("TextureClear");

            SetHologramShaderAlpha(Compositor.DefaultAlpha);

            CreateColorTexture();
            CreateOutputTextures();

            SetupCameraAndRenderTextures();

            SetShaderValues();

            SetOutputTextures();
        }

        private void Update()
        {
            // this updates after we start running or when the video source changes, so we need to check every frame
            bool newOutputYUV = UnityCompositorInterface.OutputYUV();
            if (outputYUV != newOutputYUV)
            {
                outputYUV = newOutputYUV;
            }
        }

        private void SetupCameraAndRenderTextures()
        {
            if (spectatorViewCamera != null)
            {
                Debug.LogError("Can only have a single SV camera");
            }

            spectatorViewCamera = GetComponent<Camera>();
            if (spectatorViewCamera == null)
            {
                renderTexture = null;
                return;
            }
            spectatorViewCamera.enabled = true;
            spectatorViewCamera.clearFlags = CameraClearFlags.Depth;
            spectatorViewCamera.nearClipPlane = 0.01f;
            spectatorViewCamera.backgroundColor = new Color(0, 0, 0, 0);

            supersampleBuffers = new RenderTexture[Compositor.SuperSampleLevel];
            downsampleMats = new Material[supersampleBuffers.Length];

            renderTexture = new RenderTexture(frameWidth << supersampleBuffers.Length, frameHeight << supersampleBuffers.Length, (int)Compositor.TextureDepth);
            renderTexture.antiAliasing = (int)Compositor.AntiAliasing;
            renderTexture.filterMode = Compositor.Filter;

            spectatorViewCamera.targetTexture = renderTexture;

            colorRGBTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            alphaTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            compositeTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);

            if (supersampleBuffers.Length > 0)
            {
                RenderTexture sourceTexture = renderTexture;

                for (int i = supersampleBuffers.Length - 1; i >= 0; i--)
                {
                    supersampleBuffers[i] = new RenderTexture(frameWidth << i, frameHeight << i, (int)Compositor.TextureDepth);
                    supersampleBuffers[i] = new RenderTexture(frameWidth << i, frameHeight << i, (int)Compositor.TextureDepth);
                    supersampleBuffers[i].filterMode = FilterMode.Bilinear;

                    downsampleMats[i] = new Material(downsampleMat);
                    downsampleMats[i].mainTexture = sourceTexture;
                    // offset is half the source pixel size
                    downsampleMats[i].SetFloat("HeightOffset", 1f / sourceTexture.height / 2);
                    downsampleMats[i].SetFloat("WidthOffset", 1f / sourceTexture.width / 2);

                    sourceTexture = supersampleBuffers[i];
                }

                renderTexture = supersampleBuffers[0];
            }
        }

        private void OnPreRender()
        {
            Graphics.Blit(CurrentColorTexture, colorRGBTexture, CurrentColorMaterial);

            if (IsVideoOutputQuadrantMode)
            {
                // Clear the camera's background by blitting using a shader that simply
                // clears the target texture without reading from the source texture.
                Graphics.Blit(null, spectatorViewCamera.targetTexture, textureClearMat);
            }
            else
            {
                // Set the input video source as the background for the camera
                // so that holograms are alpha-blended onto the correct texture
                Graphics.Blit(colorRGBTexture, spectatorViewCamera.targetTexture);
            }
        }

        private void OnPostRender()
        {
            displayOutputTexture.DiscardContents();

            RenderTexture sourceTexture = spectatorViewCamera.targetTexture;

            if (supersampleBuffers.Length > 0)
            {
                for (int i = supersampleBuffers.Length - 1; i >= 0; i--)
                {
                    Graphics.Blit(sourceTexture, supersampleBuffers[i], downsampleMats[i]);

                    sourceTexture = supersampleBuffers[i];
                }
            }

            // force set this every frame as it sometimes get unset somehow when alt-tabbing
            renderTexture = sourceTexture;

            if (IsVideoOutputQuadrantMode)
            {
                // composite hologram onto video
                BlitCompositeTexture(renderTexture, colorRGBTexture, compositeTexture);
            }
            else
            {
                // Render the real-world video back onto the composited frame to reduce the opacity
                // of the hologram by the appropriate amount.
                holoAlphaMat.SetTexture("_FrontTex", renderTexture);
                Graphics.Blit(sourceTexture, compositeTexture, holoAlphaMat);
            }

            Graphics.Blit(compositeTexture, displayOutputTexture, outputYUV ? RGBToYUVMat : RGBToBGRMat);

            Graphics.Blit(renderTexture, alphaTexture, extractAlphaMat);

            if (ShouldProduceQuadrantVideoFrame)
            {
                CreateQuadrantTexture();
                BlitQuadView(renderTexture, alphaTexture, colorRGBTexture, compositeTexture, quadViewOutputTexture);
            }

            // Video texture.
            if (UnityCompositorInterface.IsRecording())
            {
                videoOutputTexture.DiscardContents();

                Texture videoSourceTexture;
                if (IsVideoOutputQuadrantMode)
                {
                    videoSourceTexture = quadViewOutputTexture;
                }
                else
                {
                    videoSourceTexture = compositeTexture;
                }

                // convert composite to the format expected by our video encoder (NV12 or BGR)
                Graphics.Blit(videoSourceTexture, videoOutputTexture, hardwareEncodeVideo ? NV12VideoMat : BGRVideoMat);
            }

            TextureRenderCompleted?.Invoke();

            // push the texture to the compositor plugin and pull the next real world camera texture

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
            GL.IssuePluginEvent(renderEvent, 1);
        }

        private void BlitCompositeTexture(Texture hologramTex, Texture videoFeedTex, RenderTexture compositeTex)
        {
            alphaBlendMat.SetTexture("_BackTex", videoFeedTex);
            Graphics.Blit(hologramTex, compositeTex, alphaBlendMat);
        }

        private void BlitQuadView(
            Texture hologramTex,
            Texture hologramAlphaTex,
            Texture srcVideoTex,
            Texture compositeTex,
            RenderTexture outputTex)
        {
            quadViewMat.SetTexture("_HologramTex", hologramTex);
            quadViewMat.SetTexture("_HologramAlphaTex", hologramAlphaTex);
            quadViewMat.SetTexture("_CompositeTex", compositeTex);

            Graphics.Blit(srcVideoTex, outputTex, quadViewMat);
        }

        private void SetShaderValues()
        {
            holoAlphaMat.SetTexture("_BackTex", colorRGBTexture);

            BGRToRGBMat.SetInt("_YFlip", overrideColorTexture == null ? 1 : 0);
            BGRToRGBMat.SetFloat("_AlphaScale", 0);

            YUVToRGBMat.SetFloat("_AlphaScale", 0);
            YUVToRGBMat.SetFloat("_Width", frameWidth);
            YUVToRGBMat.SetFloat("_Height", frameHeight);

            RGBToYUVMat.SetFloat("_Width", frameWidth);
            RGBToYUVMat.SetFloat("_Height", frameHeight);

            BGRVideoMat.SetFloat("_YFlip", 0);
        }

        /// <summary>
        /// Sets the alpha value used for compositing holograms.
        /// </summary>
        /// <param name="alpha">The new alpha value for compositing.</param>
        public void SetHologramShaderAlpha(float alpha)
        {
            UnityCompositorInterface.SetAlpha(alpha);
            alphaBlendMat.SetFloat("_Alpha", alpha);
            holoAlphaMat.SetFloat("_Alpha", alpha);
        }

        public void InitializeVideoRecordingTextures()
        {
            var videoRecordingFrameWidth = Compositor.VideoRecordingFrameWidth;
            var videoRecordingFrameHeight = Compositor.VideoRecordingFrameHeight;

            NV12VideoMat.SetFloat("_Width", videoRecordingFrameWidth);
            NV12VideoMat.SetFloat("_Height", videoRecordingFrameHeight);

            // The output texture should always specify Linear read/write so that color space conversions are not performed when recording
            // the video when using Linear rendering in Unity.
            videoOutputTexture = new RenderTexture(videoRecordingFrameWidth, videoRecordingFrameHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            videoOutputTexture.filterMode = FilterMode.Point;
            videoOutputTexture.anisoLevel = 0;
            videoOutputTexture.antiAliasing = 1;
            videoOutputTexture.depth = 0;
            videoOutputTexture.useMipMap = false;

            // hack, this forces the nativetexturepointer to be assigned inside the engine
            videoOutputTexture.colorBuffer.ToString();

            UnityCompositorInterface.SetVideoRenderTexture(videoOutputTexture.GetNativeTexturePtr());
        }

        #region UnityExternalTextures
        /// <summary>
        /// Create External texture resources and poll for latest Color frame.
        /// </summary>
        private void CreateColorTexture()
        {
            if (colorTexture == null)
            {
                IntPtr colorSRV;
                if (UnityCompositorInterface.CreateUnityColorTexture(out colorSRV))
                {
                    colorTexture = Texture2D.CreateExternalTexture(frameWidth, frameHeight, TextureFormat.ARGB32, false, false, colorSRV);
                    colorTexture.filterMode = FilterMode.Point;
                    colorTexture.anisoLevel = 0;
                }
            }
        }

        private void CreateQuadrantTexture()
        {
            if (quadViewOutputTexture == null)
            {
                // The output texture should always specify Linear read/write so that color space conversions are not performed when recording
                // the video when using Linear rendering in Unity.
                quadViewOutputTexture = new RenderTexture(UnityCompositorInterface.GetVideoRecordingFrameWidth(VideoRecordingFrameLayout.Quad), UnityCompositorInterface.GetVideoRecordingFrameHeight(VideoRecordingFrameLayout.Quad), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                quadViewOutputTexture.filterMode = FilterMode.Point;
                quadViewOutputTexture.anisoLevel = 0;
                quadViewOutputTexture.antiAliasing = 1;
                quadViewOutputTexture.depth = 0;
                quadViewOutputTexture.useMipMap = false;
            }
        }

        private void CreateOutputTextures()
        {
            if (displayOutputTexture == null)
            {
                // The output texture should always specify Linear read/write so that color space conversions are not performed when recording
                // the video when using Linear rendering in Unity.
                displayOutputTexture = new RenderTexture(frameWidth, frameHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                displayOutputTexture.filterMode = FilterMode.Point;
                displayOutputTexture.anisoLevel = 0;
                displayOutputTexture.antiAliasing = 1;
                displayOutputTexture.depth = 0;
                displayOutputTexture.useMipMap = false;
            }
        }

        private void SetOutputTextures()
        {
            // hack, this forces the nativetexturepointer to be assigned inside the engine
            displayOutputTexture.colorBuffer.ToString();
            compositeTexture.colorBuffer.ToString();

            UnityCompositorInterface.SetOutputRenderTexture(displayOutputTexture.GetNativeTexturePtr());
            UnityCompositorInterface.SetHoloTexture(compositeTexture.GetNativeTexturePtr());
        }
        #endregion
#endif
    }
}
