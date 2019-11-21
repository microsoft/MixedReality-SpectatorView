// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    internal class MaterialPropertyAssetCache : AssetCache<MaterialPropertyAssetCache>
    {
        [SerializeField]
        private MaterialAsset[] materialAssets = null;

        private ILookup<string, MaterialPropertyAsset> materialPropertiesByShaderName;
        private ILookup<string, MaterialAsset> materialAssetsByShaderName;
        private ILookup<string, MaterialPropertyAsset> customMaterialPropertiesByShaderName;
        private MaterialPropertyAsset[] customInstanceShaderProperties;

        protected override void Awake()
        {
            base.Awake();

            foreach (MaterialAsset materialAsset in materialAssets)
            {
                foreach (MaterialPropertyAsset materialProperty in materialAsset.MaterialProperties)
                {
                    materialProperty.MaterialAsset = materialAsset;
                }
            }
        }

        private MaterialPropertyAsset[] universalMaterialProperties = new MaterialPropertyAsset[]
        {
            new MaterialPropertyAsset
            {
                propertyName = "renderQueue-36f16fdc-72c7-430d-9479-6c9c2a318b6b",
                propertyType = MaterialPropertyType.RenderQueue,
            },
            new MaterialPropertyAsset
            {
                propertyName = "shaderKeywords-266455c9-953b-476b-85cd-7dd3fde79381",
                propertyType = MaterialPropertyType.ShaderKeywords,
            }
        };

        private MaterialPropertyAsset[] CustomInstanceShaderProperties
        {
            get
            {
                if (customInstanceShaderProperties == null)
                {
                    customInstanceShaderProperties = CustomShaderPropertyAssetCache.LoadAssetCache<CustomShaderPropertyAssetCache>()?.CustomInstanceShaderProperties ?? Array.Empty<MaterialPropertyAsset>();
                }
                return customInstanceShaderProperties;
            }
        }

        public IEnumerable<MaterialPropertyAsset> GetMaterialProperties(string shaderName)
        {
            return universalMaterialProperties.Concat(MaterialPropertiesByShaderName[shaderName]).Concat(CustomMaterialPropertiesByShaderName[shaderName]);
        }

        private ILookup<string, MaterialAsset> MaterialAssetsByShaderName
        {
            get
            {
                return materialAssetsByShaderName ?? (materialAssetsByShaderName = (materialAssets ?? Array.Empty<MaterialAsset>()).ToLookup(m => m.ShaderName));
            }
        }

        private ILookup<string, MaterialPropertyAsset> MaterialPropertiesByShaderName
        {
            get
            {
                return materialPropertiesByShaderName ?? (materialPropertiesByShaderName = (materialAssets.SelectMany(m => m.MaterialProperties) ?? Array.Empty<MaterialPropertyAsset>()).ToLookup(m => m.ShaderName));
            }
        }

        private ILookup<string, MaterialPropertyAsset> CustomMaterialPropertiesByShaderName
        {
            get
            {
                return customMaterialPropertiesByShaderName ?? (customMaterialPropertiesByShaderName = CustomInstanceShaderProperties.ToLookup(m => m.ShaderName));
            }
        }

        public Shader GetShader(string shaderName)
        {
            Debug.Log("GetShader for " + shaderName);
            Debug.Log(materialAssets?.Length);
            return MaterialAssetsByShaderName[shaderName].Select(asset => asset.Shader).FirstOrDefault();
        }

        public override void UpdateAssetCache()
        {
#if UNITY_EDITOR
            Dictionary<string, MaterialAsset> newMaterialAssets = new Dictionary<string, MaterialAsset>();

            foreach (Renderer renderer in EnumerateAllComponentsInScenesAndPrefabs<Renderer>())
            {
                if (renderer != null && renderer.sharedMaterials != null)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        UpdateMaterial(newMaterialAssets, material);
                    }
                }
            }

            foreach (Graphic graphic in EnumerateAllComponentsInScenesAndPrefabs<Graphic>())
            {
                if (graphic.materialForRendering != null)
                {
                    UpdateMaterial(newMaterialAssets, graphic.materialForRendering);
                }
            }

            foreach (Material material in EnumerateAllAssetsInAssetDatabase<Material>(IsMaterialFileExtension))
            {
                UpdateMaterial(newMaterialAssets, material);
            }

            materialAssets = newMaterialAssets.Values.OrderBy(m => m.ShaderName).ToArray();

            foreach (MaterialAsset materialAsset in materialAssets)
            {
                materialAsset.CompleteEditing();
            }

            EditorUtility.SetDirty(this);
#endif
        }

        public override void ClearAssetCache()
        {
#if UNITY_EDITOR
            materialAssets = null;
            materialPropertiesByShaderName = null;

            EditorUtility.SetDirty(this);
#endif
        }

        private static bool IsMaterialFileExtension(string fileExtension)
        {
            return fileExtension == ".mat";
        }

#if UNITY_EDITOR
        private static void UpdateMaterial(Dictionary<string, MaterialAsset> materialAssets, Material material)
        {
            if (material != null)
            {
                if (!materialAssets.TryGetValue(material.shader.name, out MaterialAsset materialAsset))
                {
                    materialAsset = new MaterialAsset
                    {
                        Shader = material.shader
                    };
                    materialAssets.Add(material.shader.name, materialAsset);
                }

                foreach (MaterialProperty materialProperty in MaterialEditor.GetMaterialProperties(new Material[] { material }))
                {
                    materialAsset.AddProperty(materialProperty);
                }
            }
        }
#endif
    }
}