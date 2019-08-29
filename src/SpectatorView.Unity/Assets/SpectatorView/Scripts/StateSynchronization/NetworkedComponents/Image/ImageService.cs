// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class ImageService : ComponentBroadcasterService<ImageService, ImageObserver>, IAssetCacheUpdater
    {
        public static readonly ShortID ID = new ShortID("IMG");

        public override ShortID GetID() { return ID; }

        private void Start()
        {
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<ImageBroadcaster>(typeof(Image)));
        }

        public override void LerpRead(BinaryReader message, GameObject mirror, float lerpVal)
        {
            ImageObserver comp = mirror.GetComponent<ImageObserver>();
            if (comp)
            {
                comp.LerpRead(message, lerpVal);
            }
        }


        public Guid GetSpriteId(Sprite sprite)
        {
            var spriteAssets = SpriteAssetCache.Instance;
            if (spriteAssets == null)
            {
                return Guid.Empty;
            }
            else
            {
                return spriteAssets.GetAssetId(sprite);
            }
        }

        public Sprite GetSprite(Guid guid)
        {
            var spriteAssets = SpriteAssetCache.Instance;
            if (spriteAssets == null)
            {
                return null;
            }
            else
            {
                return spriteAssets.GetAsset(guid);
            }
        }

        public void UpdateAssetCache()
        {
            SpriteAssetCache.GetOrCreateAssetCache<SpriteAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            SpriteAssetCache.GetOrCreateAssetCache<SpriteAssetCache>().ClearAssetCache();
        }
    }
}
