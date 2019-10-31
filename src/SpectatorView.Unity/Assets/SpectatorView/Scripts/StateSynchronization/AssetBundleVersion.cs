// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class AssetBundleVersion : ScriptableObject
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
