// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class AssetCacheContent : ScriptableObject
    {
        [SerializeField]
        public AssetCacheEntry[] AssetCacheEntries;
    }
}
