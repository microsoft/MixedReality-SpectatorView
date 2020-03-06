// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class ImageService : ComponentBroadcasterService<ImageService, ImageObserver>, IAssetCache
    {
        public static readonly ShortID ID = new ShortID("IMG");

        public override ShortID GetID() { return ID; }

        private SpriteAssetCache spriteAssets;

        private void Start()
        {
            spriteAssets = SpriteAssetCache.LoadAssetCache<SpriteAssetCache>();
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<ImageBroadcaster>(typeof(Image)));
        }

        public override void LerpRead(BinaryReader message, GameObject mirror, float lerpVal)
        {
            ImageObserver comp = mirror.GetComponent<ImageObserver>();
            if (comp)
                comp.LerpRead(message, lerpVal);
        }


        public AssetId GetSpriteId(Sprite sprite)
        {
            return spriteAssets?.GetAssetId(sprite) ?? AssetId.Empty;
        }

        public Sprite GetSprite(AssetId assetId)
        {
            return spriteAssets?.GetAsset(assetId);
        }

        public void UpdateAssetCache()
        {
            SpriteAssetCache.GetOrCreateAssetCache<SpriteAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            SpriteAssetCache.GetOrCreateAssetCache<SpriteAssetCache>().ClearAssetCache();
        }

        public void SaveAssets()
        {
            SpriteAssetCache.GetOrCreateAssetCache<SpriteAssetCache>().SaveAssets();
        }
    }
}
