using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.SpectatorView.Tests
{
    public class CompositorTestsBase
    {
        public CompositionManager CompositionManager
        {
            get
            {
                if (compositionManager == null)
                {
                    compositionManager = GameObject.FindObjectOfType<CompositionManager>();
                }

                return compositionManager;
            }
        }
        private CompositionManager compositionManager;
        private string originalSceneName;
        protected List<string> filesToDelete = new List<string>();
        protected GameObject helperGameObject;

        [SetUp]
        public void SetUp()
        {
            originalSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene("SpectatorViewCompositor");
            helperGameObject = new GameObject("HelperGameObject");
        }

        [TearDown]
        public void TearDown()
        {
            CompositionManager.TextureManager.SetOverrideColorTexture(null);
            CompositionManager.TextureManager.SetOverrideOutputTexture(null);
            SceneManager.LoadScene(originalSceneName);
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    Debug.Log($"Deleting file: {file}");
                    File.Delete(file);
                }
            }
        }

        protected IEnumerator AssertTexturesInitialize(string captureDeviceName)
        {
            yield return CompositionManager.VideoRecordingLayout = VideoRecordingFrameLayout.Composite;
            AssertAllCompositeTexturesCreated(captureDeviceName);
            yield return CompositionManager.VideoRecordingLayout = VideoRecordingFrameLayout.Quad;
            AssertAllQuadTexturesCreated(captureDeviceName);
        }

        protected void AssertAllCompositeTexturesCreated(string captureDeviceName)
        {
            Assert.IsNotNull(CompositionManager.TextureManager.colorRGBTexture, $"{captureDeviceName}: Composite ColorRGBTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.colorRGBTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.renderTexture, $"{captureDeviceName}: Composite RenderTextrue is not null.");
            AssertTextureSize(CompositionManager.TextureManager.renderTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.compositeTexture, $"{captureDeviceName}: Composite CompositeTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.compositeTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.previewTexture, $"{captureDeviceName}: Composite PreviewTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.previewTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.alphaTexture, $"{captureDeviceName}: Composite AlphaTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.alphaTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
        }

        protected void AssertAllQuadTexturesCreated(string captureDeviceName)
        {
            Assert.IsNotNull(CompositionManager.TextureManager.colorRGBTexture, $"{captureDeviceName}: Quad ColorRGBTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.colorRGBTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.renderTexture, $"{captureDeviceName}: Quad RenderTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.renderTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.compositeTexture, $"{captureDeviceName}: Quad CompositeTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.compositeTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.previewTexture, $"{captureDeviceName}: Quad PreviewTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.previewTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.alphaTexture, $"{captureDeviceName}: Quad AlphaTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.alphaTexture, CompositionManager.GetVideoFrameWidth(), CompositionManager.GetVideoFrameHeight());
            Assert.IsNotNull(CompositionManager.TextureManager.quadViewOutputTexture, $"{captureDeviceName}: Quad QuadViewOutputTexture is not null.");
            AssertTextureSize(CompositionManager.TextureManager.quadViewOutputTexture, CompositionManager.VideoRecordingFrameWidth, CompositionManager.VideoRecordingFrameHeight);
        }

        protected void AssertTextureSize(Texture texture, int width, int height)
        {
            Assert.AreEqual(texture.width, width, "Texture Width");
            Assert.AreEqual(texture.height, height, "Texture Height");
        }
    }
}
