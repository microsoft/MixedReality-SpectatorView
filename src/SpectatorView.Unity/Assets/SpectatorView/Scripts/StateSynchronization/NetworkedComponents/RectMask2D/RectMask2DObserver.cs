// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class RectMask2DObserver : ComponentObserver<RectMask2D>
    {
        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            RectMask2DBroadcaster.ChangeType changeType = (RectMask2DBroadcaster.ChangeType)message.ReadByte();

            if (RectMask2DBroadcaster.HasFlag(changeType, RectMask2DBroadcaster.ChangeType.Properties))
            {
                if (attachedComponent == null)
                {
                    attachedComponent = gameObject.AddComponent<RectMask2D>();
                }

                attachedComponent.enabled = message.ReadBoolean();
            }
        }
    }
}
