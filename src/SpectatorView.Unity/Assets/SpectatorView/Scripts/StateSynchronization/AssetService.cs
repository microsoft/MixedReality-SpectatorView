﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class AssetService : Singleton<AssetService>, IAssetCache
    {
        private TextureAssetCache textureAssets;
        private MeshAssetCache meshAssets;
        private MaterialPropertyAssetCache materialPropertyAssets;

        private readonly Dictionary<ShortID, IAssetSerializer<Texture>> textureSerializers = new Dictionary<ShortID, IAssetSerializer<Texture>>();

        protected virtual void Start()
        {
            textureAssets = LookupAssetCache<TextureAssetCache>();
            meshAssets = LookupAssetCache<MeshAssetCache>();
            materialPropertyAssets = LookupAssetCache<MaterialPropertyAssetCache>();

            if (textureAssets != null)
            {
                textureSerializers.Add(textureAssets.GetID(), textureAssets);
            }
        }

        private T LookupAssetCache<T>() where T : AssetCache
        {
            T toReturn = AssetCache.LoadAssetCache<T>();

            if (toReturn == null)
            {
                Debug.LogWarning($"Could not find an asset cache of type: {typeof(T).Name}");
            }

            return toReturn;
        }

        public IEnumerable<MaterialPropertyAsset> GetMaterialProperties(string shaderName)
        {
            return materialPropertyAssets?.GetMaterialProperties(shaderName) ?? Array.Empty<MaterialPropertyAsset>();
        }

        public void RegisterTextureSerializer(IAssetSerializer<Texture> textureSerializer)
        {
            textureSerializers.Add(textureSerializer.GetID(), textureSerializer);
        }

        public bool TrySerializeTexture(BinaryWriter writer, Texture texture)
        {
            foreach (KeyValuePair<ShortID, IAssetSerializer<Texture>> serializerPair in textureSerializers)
            {
                if (serializerPair.Value.CanSerialize(texture))
                {
                    writer.Write(serializerPair.Key.Value);
                    serializerPair.Value.Serialize(writer, texture);
                    return true;
                }
            }

            writer.Write((ushort)0);
            return false;
        }

        public bool TryDeserializeTexture(BinaryReader reader, out Texture texture)
        {
            ShortID shortID = new ShortID(reader.ReadUInt16());

            IAssetSerializer<Texture> textureSerializer;
            if (textureSerializers.TryGetValue(shortID, out textureSerializer))
            {
                texture = textureSerializer.Deserialize(reader);
                return true;
            }
            else
            {
                texture = null;
                return false;
            }
        }

        public AssetId GetMeshId(Mesh mesh)
        {
            return meshAssets?.GetAssetId(mesh) ?? AssetId.Empty;
        }

        public bool AttachMeshFilter(GameObject gameObject, AssetId assetId)
        {
            ComponentExtensions.EnsureComponent<MeshRenderer>(gameObject);

            Mesh mesh = meshAssets.GetAsset(assetId);
            if (mesh != null)
            {
                MeshFilter filter = ComponentExtensions.EnsureComponent<MeshFilter>(gameObject);
                filter.sharedMesh = mesh;
                return true;
            }

            return false;
        }

        public bool AttachDynamicMeshFilter(GameObject gameObject, AssetId assetId, Mesh mesh)
        {
            ComponentExtensions.EnsureComponent<MeshRenderer>(gameObject);
            MeshFilter filter = ComponentExtensions.EnsureComponent<MeshFilter>(gameObject);
            filter.sharedMesh = mesh;
            return true;
        }

        public bool AttachSkinnedMeshRenderer(GameObject gameObject, AssetId assetId)
        {
            Mesh mesh = meshAssets.GetAsset(assetId);
            if (mesh != null)
            {
                SkinnedMeshRenderer renderer = ComponentExtensions.EnsureComponent<SkinnedMeshRenderer>(gameObject);
                renderer.sharedMesh = mesh;
                return true;
            }

            return false;
        }

        public void UpdateAssetCache()
        {
            AssetCache.GetOrCreateAssetCache<TextureAssetCache>().UpdateAssetCache();
            AssetCache.GetOrCreateAssetCache<MeshAssetCache>().UpdateAssetCache();
            AssetCache.GetOrCreateAssetCache<MaterialPropertyAssetCache>().UpdateAssetCache();
            AssetCache.GetOrCreateAssetCache<CustomShaderPropertyAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            AssetCache.GetOrCreateAssetCache<TextureAssetCache>().ClearAssetCache();
            AssetCache.GetOrCreateAssetCache<MeshAssetCache>().ClearAssetCache();
            AssetCache.GetOrCreateAssetCache<MaterialPropertyAssetCache>().ClearAssetCache();
            AssetCache.GetOrCreateAssetCache<CustomShaderPropertyAssetCache>().ClearAssetCache();
        }

        public void SaveAssets()
        {
            AssetCache.GetOrCreateAssetCache<TextureAssetCache>().SaveAssets();
            AssetCache.GetOrCreateAssetCache<MeshAssetCache>().SaveAssets();
            AssetCache.GetOrCreateAssetCache<MaterialPropertyAssetCache>().SaveAssets();
            AssetCache.GetOrCreateAssetCache<CustomShaderPropertyAssetCache>().SaveAssets();
        }
    }
}