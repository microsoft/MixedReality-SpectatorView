// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface IEditableSpatialLocalizationSettings
    {
#if UNITY_EDITOR
        SpatialLocalizationSettingsEditor CreateEditor();
#endif
    }
}