// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class CanvasRendererObserver : ComponentObserver<CanvasRenderer>
    {
        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            CanvasRendererBroadcaster.ChangeType changeType = (CanvasRendererBroadcaster.ChangeType)message.ReadByte();

            if (CanvasRendererBroadcaster.HasFlag(changeType, CanvasRendererBroadcaster.ChangeType.Properties))
            {
                attachedComponent.SetAlpha(message.ReadSingle());
                attachedComponent.SetColor(message.ReadColor());
            }
        }
    }
}
