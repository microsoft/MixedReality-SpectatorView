// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class TextMeshService : ComponentBroadcasterService<TextMeshService, TextMeshObserver>, IAssetCache
    {
        public static readonly ShortID ID = new ShortID("TXT");

        public override ShortID GetID() { return ID; }

        private FontAssetCache fontAssets;
        
        private void Start()
        {
            fontAssets = FontAssetCache.LoadAssetCache<FontAssetCache>();
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<TextMeshBroadcaster>(typeof(TextMesh), typeof(MeshRenderer)));
        }

        public AssetId GetFontId(Font font)
        {
            return fontAssets?.GetAssetId(font) ?? AssetId.Empty;
        }

        public Font GetFont(AssetId assetId)
        {
            return fontAssets?.GetAsset(assetId);
        }

        public void UpdateAssetCache()
        {
            FontAssetCache.GetOrCreateAssetCache<FontAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            FontAssetCache.GetOrCreateAssetCache<FontAssetCache>().ClearAssetCache();
        }

        public void SaveAssets()
        {
            FontAssetCache.GetOrCreateAssetCache<FontAssetCache>().SaveAssets();
        }
    }
}
