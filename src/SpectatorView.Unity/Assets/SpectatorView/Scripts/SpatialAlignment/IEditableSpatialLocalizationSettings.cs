// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;

namespace Microsoft.MixedReality.SpectatorView
{
#if UNITY_EDITOR
    public interface IEditableSpatialLocalizationSettings
    {
        SpatialLocalizationSettingsEditor CreateEditor();
    }
#endif
}