// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class AssetBundleVersion : ScriptableObject
    {
        public string Identity;
        public string DisplayName;

        public override string ToString()
        {
            return Format(Identity, DisplayName);
        }

        public static string Format(string identity, string displayName)
        {
            return $"\"{displayName}\" ({identity})";
        }
    }
}
