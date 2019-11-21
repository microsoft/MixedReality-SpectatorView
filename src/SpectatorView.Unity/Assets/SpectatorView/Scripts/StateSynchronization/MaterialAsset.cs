// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    internal class MaterialAsset
    {
        public Shader shader;

        [SerializeField]
        private string shaderName;

        [SerializeField]
        private MaterialPropertyAsset[] materialProperties = null;

        public IReadOnlyList<MaterialPropertyAsset> MaterialProperties => materialProperties;
        private Dictionary<string, MaterialPropertyAsset> materialPropertyAssetLookup;

        public Shader Shader
        {
            get { return shader; }
            set
            {
                shader = value;
                shaderName = shader?.name;
            }
        }

        public string ShaderName
        {
            get { return shaderName; }
        }

#if UNITY_EDITOR
        public void AddProperty(MaterialProperty materialProperty)
        {
            if (materialPropertyAssetLookup == null)
            {
                materialPropertyAssetLookup = (materialProperties ?? Array.Empty<MaterialPropertyAsset>()).ToDictionary(p => p.propertyName);
            }

            if (!materialPropertyAssetLookup.ContainsKey(materialProperty.name))
            {
                materialPropertyAssetLookup.Add(materialProperty.name, new MaterialPropertyAsset
                {
                    propertyName = materialProperty.name,
                    propertyType = (MaterialPropertyType)materialProperty.type
                });
            }
        }

        public void CompleteEditing()
        {
            materialProperties = materialPropertyAssetLookup?.Values.OrderBy(p => p.propertyName).ToArray();
        }
#endif
    }
}