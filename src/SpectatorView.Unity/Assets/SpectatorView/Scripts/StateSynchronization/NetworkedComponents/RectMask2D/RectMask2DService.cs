// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class RectMask2DService : ComponentBroadcasterService<RectMask2DService, RectMask2DObserver>
    {
        public static readonly ShortID ID = new ShortID("RM2");

        public override ShortID GetID() { return ID; }

        private void Start()
        {
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<RectMask2DBroadcaster>(typeof(RectMask2D)));
        }
    }
}
