using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public enum AssetBundlePlatform
    {
        Unknown,
        WSA,
        Android,
        iOS
    }

    public static class AssetBundlePlatformInfo
    {
        public static AssetBundlePlatform Current
        {
            get
            {
#if UNITY_WSA
                return AssetBundlePlatform.WSA;
#elif UNITY_ANDROID
                return AssetBundlePlatform.Android;
#elif UNITY_IOS
                return AssetBundlePlatform.iOS;
#else
                return AssetBundlePlatform.Unknown;
#endif
            }
        }
    }
}