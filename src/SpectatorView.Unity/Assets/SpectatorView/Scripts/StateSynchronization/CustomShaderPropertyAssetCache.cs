// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class CustomShaderPropertyAssetCache : AssetCache
    {
        [SerializeField]
        private GlobalMaterialPropertyAsset[] customGlobalShaderProperties = null;

        [SerializeField]
        private MaterialPropertyAsset[] customInstanceShaderProperties = null;

        public GlobalMaterialPropertyAsset[] CustomGlobalShaderProperties
        {
            get { return customGlobalShaderProperties ?? Array.Empty<GlobalMaterialPropertyAsset>(); }
        }

        public MaterialPropertyAsset[] CustomInstanceShaderProperties
        {
            get { return customInstanceShaderProperties ?? Array.Empty<MaterialPropertyAsset>(); }
        }

        // TODO - consider if this function should be defined in base class
        public void SaveAssets() { }
    }
}