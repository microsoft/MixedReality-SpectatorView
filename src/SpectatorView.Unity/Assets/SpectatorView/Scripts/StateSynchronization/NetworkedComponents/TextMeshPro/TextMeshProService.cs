// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using TMPro;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class TextMeshProService : ComponentBroadcasterService<TextMeshProService, TextMeshProObserver>, IAssetCache
    {
        public static readonly ShortID ID = new ShortID("TMP");

        public override ShortID GetID() { return ID; }

        private TextMeshProFontAssetCache fontAssets;

        private void Start()
        {
            fontAssets = TextMeshProFontAssetCache.LoadAssetCache<TextMeshProFontAssetCache>();
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<TextMeshProBroadcaster>(typeof(TextMeshPro)));
        }

        public AssetId GetFontId(TMP_FontAsset font)
        {
            return fontAssets?.GetAssetId(font) ?? AssetId.Empty;
        }

        public TMP_FontAsset GetFont(AssetId assetId)
        {
            return (TMP_FontAsset)fontAssets?.GetAsset(assetId);
        }

        public void UpdateAssetCache()
        {
            TextMeshProFontAssetCache.GetOrCreateAssetCache<TextMeshProFontAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            TextMeshProFontAssetCache.GetOrCreateAssetCache<TextMeshProFontAssetCache>().ClearAssetCache();
        }

        public void SaveAssets()
        {
            TextMeshProFontAssetCache.GetOrCreateAssetCache<TextMeshProFontAssetCache>().SaveAssets();
        }
    }
}
