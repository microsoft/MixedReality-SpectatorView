// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView;
using MaterialPropertyBlock = Microsoft.MixedReality.SpectatorView.MaterialPropertyBlock;

namespace Microsoft.MixedReality.Demo
{
    public class ChangeMaterialProperty : MonoBehaviour
    {
        [SerializeField]
        private Renderer targetRenderer = null;

        private int propertyID;

        private void Awake()
        {
            propertyID = Shader.PropertyToID("_Color");
        }

        private void Update()
        {
            float r = (1.0f + Mathf.Sin(Time.time)) / 2.0f;
            float g = (1.0f + Mathf.Sin(2 * Time.time)) / 2.0f;
            float b = (1.0f + Mathf.Sin(3 * Time.time)) / 2.0f;
            float alpha = (1.0f + Mathf.Sin(4 * Time.time)) / 2.0f;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor(propertyID, new Color(r, g, b, alpha));
            targetRenderer.SetPropertyBlock(block);
        }
    }
}