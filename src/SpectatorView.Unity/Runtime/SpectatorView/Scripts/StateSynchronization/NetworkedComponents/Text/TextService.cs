// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class TextService : ComponentBroadcasterService<TextService, TextObserver>
    {
        public static readonly ShortID ID = new ShortID("UTX");

        public override ShortID GetID() { return ID; }

        private FontAssetCache fontAssets;

        private void Start()
        {
            fontAssets = FontAssetCache.LoadAssetCache<FontAssetCache>();
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<TextBroadcaster>(typeof(Text)));
        }

        public AssetId GetFontId(Font font)
        {
            return fontAssets?.GetAssetId(font) ?? AssetId.Empty;
        }

        public Font GetFont(AssetId assetId)
        {
            return fontAssets?.GetAsset(assetId);
        }
    }
}
