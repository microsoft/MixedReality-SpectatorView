// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
#if UNITY_EDITOR
    public abstract class SpatialLocalizationSettingsEditor : PopupWindowContent
    {
        public event Action<SpatialLocalizationSettingsEditor> EditingCompleted;

        public override void OnClose()
        {
            base.OnClose();

            EditingCompleted?.Invoke(this);
        }
    }
#endif
}