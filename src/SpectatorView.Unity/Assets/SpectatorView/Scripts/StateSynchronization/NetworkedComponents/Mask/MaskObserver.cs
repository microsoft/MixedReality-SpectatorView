// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class MaskObserver : ComponentObserver<Mask>
    {
        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            MaskBroadcaster.ChangeType changeType = (MaskBroadcaster.ChangeType)message.ReadByte();

            if (MaskBroadcaster.HasFlag(changeType, MaskBroadcaster.ChangeType.Properties))
            {
                attachedComponent.enabled = message.ReadBoolean();
                attachedComponent.showMaskGraphic = message.ReadBoolean();
            }
        }
    }
}
