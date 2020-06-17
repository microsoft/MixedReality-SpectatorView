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
    [Serializable]
    public class ColorCorrection
    {
        public bool Enabled;

        [Range(0, 4)]
        public float RScale;

        [Range(0, 4)]
        public float GScale;

        [Range(0, 4)]
        public float BScale;

        [Range(-1, 1)]
        public float HOffset;

        [Range(-1, 1)]
        public float SOffset;

        [Range(-1, 1)]
        public float VOffset;

        [Range(-1, 1)]
        public float Brightness;

        [Range(0, 2)]
        public float Contrast;

        [Range(0.1f, 4)]
        public float Gamma;

        public ColorCorrection(bool enabled)
        {
            this.Enabled = enabled;
            this.RScale = 1;
            this.GScale = 1;
            this.BScale = 1;
            this.HOffset = 0;
            this.SOffset = 0;
            this.VOffset = 0;
            this.Brightness = 0;
            this.Contrast = 1;
            this.Gamma = 1;
        }

        public void ApplyParameters(Material material)
        {
            if (material != null)
            {
                material.SetFloat("_RScale", RScale);
                material.SetFloat("_GScale", GScale);
                material.SetFloat("_BScale", BScale);
                material.SetFloat("_HOffset", HOffset);
                material.SetFloat("_SOffset", SOffset);
                material.SetFloat("_VOffset", VOffset);
                material.SetFloat("_Brightness", Brightness);
                material.SetFloat("_Contrast", Contrast);
                material.SetFloat("_Gamma", Gamma);
            }
        }

        public static ColorCorrection GetColorCorrection(string namePrefix)
        {
            ColorCorrection output = new ColorCorrection(false);
            if (!PlayerPrefs.HasKey($"{namePrefix}.{nameof(Enabled)}"))
            {
                return output;
            }

            output.Enabled = PlayerPrefs.GetInt($"{namePrefix}.{nameof(Enabled)}") > 0;
            output.RScale = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(RScale)}");
            output.GScale = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(GScale)}");
            output.BScale = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(BScale)}");
            output.HOffset = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(HOffset)}");
            output.SOffset = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(SOffset)}");
            output.VOffset = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(VOffset)}");
            output.Brightness = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(Brightness)}");
            output.Contrast = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(Contrast)}");
            output.Gamma = PlayerPrefs.GetFloat($"{namePrefix}.{nameof(Gamma)}");
            return output;
        }

        public static void StoreColorCorrection(string namePrefix, ColorCorrection colorCorrection)
        {
            PlayerPrefs.SetInt($"{namePrefix}.{nameof(Enabled)}", colorCorrection.Enabled ? 1 : 0);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(RScale)}", colorCorrection.RScale);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(GScale)}", colorCorrection.GScale);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(BScale)}", colorCorrection.BScale);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(HOffset)}", colorCorrection.HOffset);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(SOffset)}", colorCorrection.SOffset);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(VOffset)}", colorCorrection.VOffset);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(Brightness)}", colorCorrection.Brightness);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(Contrast)}", colorCorrection.Contrast);
            PlayerPrefs.SetFloat($"{namePrefix}.{nameof(Gamma)}", colorCorrection.Gamma);
        }
    }

    /// <summary>
    /// Manages the textures used for compositing holograms with video, and controls
    /// the actual composition of textures together.
    /// </summary>
    public class TextureManager : MonoBehaviour
    {
#if UNITY_EDITOR
        public CompositionManager Compositor { get; set; }

        /// <summary>
        /// Gets or sets whether or not the quadrant video recording is required.
        /// The quadrant frame will not be rendered when this is not set and when
        /// the video recording mode does not require it.
        /// </summary>
        public bool IsQuadrantVideoFrameNeededForPreviewing { get; set; }

        /// <summary>
        /// Gets or sets whether or not the occlusion mask is required.
        /// This value ensures an occlusion mask is generate when quad recording is enabled.
        /// </summary>
        public bool IsOcclusionMaskNeededForPreviewing { get; set; }

        /// <summary>
        /// The color image texture coming from the camera, converted to RGB. The Unity camera is "cleared" to this texture.
        /// Note: If color correction is applied to the video feed, this texture will contain the color correction.
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
        /// The texture that is output to the capture card.
        /// </summary>
        public Texture previewTexture => overrideOutputTexture == null ? compositeTexture : overrideOutputTexture;

        /// <summary>
        /// An RGBA texture where all 4 channels contain the hologram alpha value
        /// </summary>
        public RenderTexture alphaTexture { get; private set; }

        /// <summary>
        /// The texture containing the raw video, alpha mask, hologram, and composite textures.
        /// </summary>
        public RenderTexture quadViewOutputTexture { get; private set; }

        /// <summary>
        /// The texture used to contain result of running Blur shader over the occulsion mask
        /// </summary>
        public RenderTexture blurOcclusionTexture { get; private set; }

        /// <summary>
        /// The raw color image data coming from the capture card
        /// </summary>
        private Texture2D colorTexture = null;

        /// <summary>
        /// The texture used to occlude holograms with the real world via depth comparison
        /// </summary>
        private Texture2D depthTexture = null;

        /// <summary>
        /// The texture used to occlude holograms with a body mask
        /// </summary>
        private Texture2D bodyMaskTexture = null;

        /// <summary>
        /// The texture used to occlude holograms with the real world
        /// </summary>
        private RenderTexture occlusionMaskTexture = null;

        /// <summary>
        /// An override texture for testing calibration
        /// </summary>
        private Texture2D overrideColorTexture = null;

        /// <summary>
        /// An override texture that will be used instead of the composited texture for recording. This texture will also be provided to the capture card output.
        /// </summary>
        private Texture overrideOutputTexture = null;

        /// <summary>
        /// The final composite texture converted to NV12 format for use in creating video file
        /// </summary>
        private RenderTexture videoOutputTexture = null;

        /// <summary>
        /// The final composite texture converted into the format expected by output on the capture card (YUV or BGRA)
        /// </summary>
        private RenderTexture displayOutputTexture = null;

        /// <summary>
        /// Gets whether or not holograms should be rendered on a black background for video recording (which allows for post-processing in quadrant mode)
        /// or against the video background (which allows for correct alpha blending for partially-transparent holograms).
        /// </summary>
        private bool IsVideoRecordingQuadrantMode => Compositor != null && Compositor.VideoRecordingLayout == VideoRecordingFrameLayout.Quad;

        private bool ShouldProduceQuadrantVideoFrame => IsQuadrantVideoFrameNeededForPreviewing || IsVideoRecordingQuadrantMode;

        public RenderTexture[] supersampleBuffers;

        public event Action TextureRenderCompleted;

        public ColorCorrection videoFeedColorCorrection { get; set; }
        public float blurSize { get; set; } = 5;
        public int numBlurPasses { get; set; } = 1;

        private Material ignoreAlphaMat;
        private Material BGRToRGBMat;
        private Material RGBToBGRMat;
        private Material YUVToRGBMat;
        private Material RGBToYUVMat;
        private Material NV12VideoMat;
        private Material BGRVideoMat;
        private Material holoAlphaMat;
        private Material blurMat;
        private Material occlusionMaskMat;
        private Material quadViewMat;
        private Material alphaBlendMat;
        private Material textureClearMat;
        private Material extractAlphaMat;
        private Material downsampleMat;
        private Material[] downsampleMats;
        private Material colorCorrectionMat;

        private Camera spectatorViewCamera;

        private int frameWidth;
        private int frameHeight;
        private bool providesYUV;
        private bool expectsYUV;
        private bool hardwareEncodeVideo;
        private IntPtr renderEvent;

        private RenderTexture videoFeedColorCorrectionTexture;
        private const string VideoFeedColorCorrectionPlayerPrefName = "VideoFeedColorCorrection";

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
                if (overrideColorTexture == null && providesYUV)
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

        /// <summary>
        /// Sets a texture that will output to recordings and the capture card output.
        /// </summary>
        /// <param name="texture">A texture that overrides the output recording/capture card texture, or null to resume outputting the
        /// composited texture to recordings and the capture card.</param>
        public void SetOverrideOutputTexture(Texture texture)
        {
            overrideOutputTexture = texture;
        }

        private void Start()
        {
            frameWidth = UnityCompositorInterface.GetFrameWidth();
            frameHeight = UnityCompositorInterface.GetFrameHeight();
            providesYUV = UnityCompositorInterface.ProvidesYUV();
            expectsYUV = UnityCompositorInterface.ExpectsYUV();
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
            blurMat = LoadMaterial("Blur");
            occlusionMaskMat = LoadMaterial("OcclusionMask");
            extractAlphaMat = LoadMaterial("ExtractAlpha");
            ignoreAlphaMat = LoadMaterial("IgnoreAlpha");
            quadViewMat = LoadMaterial("QuadView");
            alphaBlendMat = LoadMaterial("AlphaBlend");
            textureClearMat = LoadMaterial("TextureClear");
            colorCorrectionMat = LoadMaterial("ColorCorrection");

            videoFeedColorCorrection = ColorCorrection.GetColorCorrection(VideoFeedColorCorrectionPlayerPrefName);
            blurSize = PlayerPrefs.GetFloat($"{nameof(TextureManager)}.{nameof(blurSize)}", 5);
            numBlurPasses = PlayerPrefs.GetInt($"{nameof(TextureManager)}.{nameof(numBlurPasses)}", 1);

            SetHologramShaderAlpha(Compositor.DefaultAlpha);

            CreateColorTexture();
            
           if(Compositor.OcclusionMode == OcclusionSetting.RawDepthCamera)
            {
                CreateDepthCameraTexture();
            }
            else if(Compositor.OcclusionMode == OcclusionSetting.BodyTracking)
            {
                CreateDepthCameraTexture();
                CreateBodyDepthTexture();
            }

            CreateOutputTextures();

            SetupCameraAndRenderTextures();

            SetShaderValues();

            SetOutputTextures();
        }

        private void Update()
        {
            // this updates after we start running or when the video source changes, so we need to check every frame
            providesYUV = UnityCompositorInterface.ProvidesYUV();
            expectsYUV = UnityCompositorInterface.ExpectsYUV();
        }

        private void OnDestroy()
        {
            ColorCorrection.StoreColorCorrection(VideoFeedColorCorrectionPlayerPrefName, videoFeedColorCorrection);
            PlayerPrefs.SetFloat($"{nameof(TextureManager)}.{nameof(blurSize)}", blurSize);
            PlayerPrefs.SetInt($"{nameof(TextureManager)}.{nameof(numBlurPasses)}", numBlurPasses);
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
            spectatorViewCamera.depthTextureMode = DepthTextureMode.Depth;

            supersampleBuffers = new RenderTexture[Compositor.SuperSampleLevel];
            downsampleMats = new Material[supersampleBuffers.Length];

            renderTexture = new RenderTexture(frameWidth << supersampleBuffers.Length, frameHeight << supersampleBuffers.Length, (int)Compositor.TextureDepth);
            renderTexture.antiAliasing = (int)Compositor.AntiAliasing;
            renderTexture.filterMode = Compositor.Filter;

            spectatorViewCamera.targetTexture = renderTexture;

            colorRGBTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            videoFeedColorCorrectionTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            alphaTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            compositeTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            occlusionMaskTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);
            blurOcclusionTexture = new RenderTexture(frameWidth, frameHeight, (int)Compositor.TextureDepth);

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
            if (videoFeedColorCorrection.Enabled)
            {
                // Apply color correction to the video feed if it is enabled
                Graphics.Blit(CurrentColorTexture, videoFeedColorCorrectionTexture, CurrentColorMaterial);
                videoFeedColorCorrection.ApplyParameters(colorCorrectionMat);
                colorCorrectionMat.SetTexture("_MainTex", videoFeedColorCorrectionTexture);
                Graphics.Blit(videoFeedColorCorrectionTexture, colorRGBTexture, colorCorrectionMat);
            }
            else
            {
                Graphics.Blit(CurrentColorTexture, colorRGBTexture, CurrentColorMaterial);
            }

            if (IsVideoRecordingQuadrantMode)
            {
                // Clear the camera's background by blitting using a shader that simply
                // clears the target texture without reading from the source texture.
                // This is applicable for video recording in quadrant mode or for
                // showing a preview of the quadrant mode on screen.
                Graphics.Blit(null, spectatorViewCamera.targetTexture, textureClearMat);
            }
            else
            {
                // Set the input video source as the background for the camera
                // so that holograms are alpha-blended onto the correct texture
                Graphics.Blit(colorRGBTexture, spectatorViewCamera.targetTexture);
            }
        }

        private IEnumerator OnPostRender()
        {
            RenderTexture sourceTexture = spectatorViewCamera.targetTexture;

            // Capture the depth mask before calling WaitForEndOfFrame to make sure that post processing effects
            // haven't changed the depth buffer.
            if (IsOcclusionMaskNeededForPreviewing ||
                !IsVideoRecordingQuadrantMode)
            {
                occlusionMaskMat.SetTexture("_DepthTexture", depthTexture);
                occlusionMaskMat.SetTexture("_BodyMaskTexture", bodyMaskTexture);
                Graphics.Blit(sourceTexture, occlusionMaskTexture, occlusionMaskMat);

                blurMat.SetFloat("_BlurSize", blurSize);
                for (int i = 0; i < numBlurPasses || i < 1; i++)
                {
                    var source = i % 2 == 0 ? occlusionMaskTexture : blurOcclusionTexture;
                    var dest = i % 2 == 0 ? blurOcclusionTexture : occlusionMaskTexture;
                    blurMat.SetTexture("_MaskTexture", source);
                    Graphics.Blit(source, dest, blurMat);
                }

                if (numBlurPasses % 2 == 0)
                {
                    Graphics.Blit(occlusionMaskTexture, blurOcclusionTexture);
                }
            }

            yield return new WaitForEndOfFrame();

            displayOutputTexture.DiscardContents();

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

            if (IsVideoRecordingQuadrantMode)
            {
                // Composite hologram onto video for recording quadrant mode video, or for previewing
                // that quadrant-mode video on screen.
                BlitCompositeTexture(renderTexture, colorRGBTexture, compositeTexture);
            }
            else
            {
                // Render the real-world video back onto the composited frame to reduce the opacity
                // of the hologram by the appropriate amount.
                holoAlphaMat.SetTexture("_FrontTex", renderTexture);
                holoAlphaMat.SetTexture("_OcclusionTexture", blurOcclusionTexture);
                Graphics.Blit(sourceTexture, compositeTexture, holoAlphaMat);
            }

            // If an output texture override has been specified, use it instead of the composited texture
            Texture outputTexture = (overrideOutputTexture == null) ? compositeTexture : overrideOutputTexture;

            Graphics.Blit(outputTexture, displayOutputTexture, expectsYUV ? RGBToYUVMat : RGBToBGRMat);

            Graphics.Blit(renderTexture, alphaTexture, extractAlphaMat);

            if (ShouldProduceQuadrantVideoFrame)
            {
                CreateQuadrantTexture();
                BlitQuadView(renderTexture, alphaTexture, colorRGBTexture, outputTexture, quadViewOutputTexture);
            }
            
            // Video texture.
            if (UnityCompositorInterface.IsRecording())
            {
                videoOutputTexture.DiscardContents();

                Texture videoSourceTexture;
                if (IsVideoRecordingQuadrantMode)
                {
                    videoSourceTexture = quadViewOutputTexture;
                }
                else
                {
                    videoSourceTexture = outputTexture;
                }

                // convert composite to the format expected by our video encoder (NV12 or BGR)
                //Graphics.Blit(videoSourceTexture, videoOutputTexture, hardwareEncodeVideo ? NV12VideoMat : BGRVideoMat);
                Graphics.Blit(videoSourceTexture, videoOutputTexture, BGRVideoMat);
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

            BGRVideoMat.SetFloat("_YFlip", 1);
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

        private void CreateDepthCameraTexture()
        {
            if (depthTexture == null)
            {
                IntPtr depthSRV;
                if (UnityCompositorInterface.CreateUnityDepthCameraTexture(out depthSRV))
                {
                    depthTexture = Texture2D.CreateExternalTexture(frameWidth, frameHeight, TextureFormat.R16, false, false, depthSRV);
                    depthTexture.filterMode = FilterMode.Point;
                    depthTexture.anisoLevel = 0;
                }
            }
        }

        private void CreateBodyDepthTexture()
        {
            if (bodyMaskTexture == null)
            {
                IntPtr bodySRV;
                if (UnityCompositorInterface.CreateUnityBodyMaskTexture(out bodySRV))
                {
                    bodyMaskTexture = Texture2D.CreateExternalTexture(frameWidth, frameHeight, TextureFormat.R16, false, false, bodySRV);
                    bodyMaskTexture.filterMode = FilterMode.Point;
                    bodyMaskTexture.anisoLevel = 0;
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
