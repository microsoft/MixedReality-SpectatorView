// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Microsoft.MixedReality.SpectatorView.Tests
{
    public class TextureManagerTests : CompositorTestsBase
    {
        [UnityTest]
        public IEnumerator OverrideColorTextureTest()
        {
            RenderTexture origTexture = CompositionManager.TextureManager.colorRGBTexture;
            CompositionManager.TextureManager.SetOverrideColorTexture(Texture2D.whiteTexture);
            yield return null;
            AssertTextureIsColor(CompositionManager.TextureManager.colorRGBTexture, Color.white);
            CompositionManager.TextureManager.SetOverrideColorTexture(null);
            yield return null;
            AssertTexturesAreEqual(origTexture, CompositionManager.TextureManager.colorRGBTexture);
        }

        [UnityTest]
        public IEnumerator OverrideOutputTextureTest()
        {
            Texture origTexture = CompositionManager.TextureManager.previewTexture;
            CompositionManager.TextureManager.SetOverrideOutputTexture(Texture2D.whiteTexture);
            yield return null;
            AssertTextureIsColor(CompositionManager.TextureManager.previewTexture, Color.white);
            CompositionManager.TextureManager.SetOverrideOutputTexture(null);
            yield return null;
            AssertTexturesAreEqual(origTexture, CompositionManager.TextureManager.previewTexture);
        }

        private void AssertTextureIsColor(Texture texture, Color color)
        {
            Texture2D pixelHelper = GetTexture2D(texture);
            Color[] pixels = pixelHelper.GetPixels();
            bool badPixelFound = false;
            for(int i = 0; i < pixels.Length; i++)
            {
                if (!ColorComponentsAreEqual(pixels[i], color))
                {
                    badPixelFound = true;
                    break;
                }
            }

            Assert.IsFalse(badPixelFound, "Pixels are all in the provided color.");
        }

        private void AssertTexturesAreEqual(Texture a, Texture b)
        {
            Texture2D pixelAHelper = GetTexture2D(a);
            Texture2D pixelBHelper = GetTexture2D(b);
            Assert.AreEqual(pixelAHelper.width, pixelBHelper.width, "Textures have same width.");
            Assert.AreEqual(pixelAHelper.height, pixelBHelper.height, "Textures have same height.");

            Color[] pixelsA = pixelAHelper.GetPixels();
            Color[] pixelsB = pixelBHelper.GetPixels();
            bool pixelMismatchFound = false;
            for (int i = 0; i < pixelsA.Length && i < pixelsB.Length; i++)
            {
                if (pixelsA[i] != pixelsB[i])
                {
                    pixelMismatchFound = true;
                    break;
                }
            }

            Assert.IsFalse(pixelMismatchFound, "Pixels matched for both textures. ");
        }

        private Texture2D GetTexture2D(Texture texture)
        {
            if ((texture as Texture2D) != null)
            {
                return (texture as Texture2D);
            }

            if ((texture as RenderTexture) != null)
            {
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = (texture as RenderTexture);
                Texture2D texture2D = new Texture2D(texture.width, texture.height);
                texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                texture2D.Apply();
                RenderTexture.active = prevActive;
                return texture2D;
            }

            throw new Exception("Unknown texture type.");
        }

        private bool ColorComponentsAreEqual(Color a, Color b)
        {
            return (a.r == b.r) && (a.g == b.g) && (a.b == b.b);
        }
    }
}
