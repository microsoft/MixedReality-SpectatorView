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
    internal class MaterialAsset : ISerializationCallbackReceiver
    {
        public Shader shader;

        [SerializeField]
        private string shaderName;

        [SerializeField]
        private MaterialPropertyAsset[] materialProperties = null;

        private bool areMaterialPropertiesInitialized = false;
        private Dictionary<string, MaterialPropertyAsset> materialPropertyAssetLookup;

        public IReadOnlyList<MaterialPropertyAsset> MaterialProperties
        {
            get
            {
                if (!areMaterialPropertiesInitialized)
                {
                    InitializeMaterialProperties();
                }

                return materialProperties;
            }
        }

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

        private void InitializeMaterialProperties()
        {
            if (materialProperties != null)
            {
                foreach (var materialProperty in materialProperties)
                {
                    materialProperty.MaterialAsset = this;
                }
            }
            areMaterialPropertiesInitialized = true;
        }

        public void OnBeforeSerialize()
        {
            materialProperties = materialPropertyAssetLookup?.Values.OrderBy(p => p.propertyName).ToArray();
        }

        public void OnAfterDeserialize()
        {
            materialPropertyAssetLookup = (materialProperties ?? Array.Empty<MaterialPropertyAsset>()).ToDictionary(p => p.propertyName);
        }

#if UNITY_EDITOR
        public void AddProperty(MaterialProperty materialProperty)
        {
            if (materialPropertyAssetLookup == null)
            {
                materialPropertyAssetLookup = (materialProperties ?? Array.Empty<MaterialPropertyAsset>()).ToDictionary(p => p.propertyName);
            }

            // Mark this property as dirty, so that next time the public property is accessed or serialization occurs
            // the property is re-populated with sorted values
            materialProperties = null;

            if (!materialPropertyAssetLookup.ContainsKey(materialProperty.name))
            {
                materialPropertyAssetLookup.Add(materialProperty.name, new MaterialPropertyAsset
                {
                    propertyName = materialProperty.name,
                    propertyType = (MaterialPropertyType)materialProperty.type,
                    MaterialAsset = this
                });
            }
        }
#endif
    }
}