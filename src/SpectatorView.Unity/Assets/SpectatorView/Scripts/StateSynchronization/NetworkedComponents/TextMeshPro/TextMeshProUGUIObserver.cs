// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using TMPro;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class TextMeshProUGUIObserver : TextMeshProObserverBase
    {
        protected override void EnsureTextComponent()
        {
            if (TextMeshObserver == null)
            {
                RectTransformBroadcaster srt = new RectTransformBroadcaster();
                RectTransform rectTransform = GetComponent<RectTransform>();
                srt.Copy(rectTransform);
                TextMeshObserver = ComponentExtensions.EnsureComponent<TextMeshProUGUI>(gameObject);
                srt.Apply(rectTransform);
            }
        }

        public override Type ComponentType => typeof(TextMeshProUGUI);
    }
}