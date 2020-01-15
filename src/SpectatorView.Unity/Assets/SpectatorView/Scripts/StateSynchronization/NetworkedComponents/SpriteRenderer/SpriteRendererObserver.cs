// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class SpriteRendererObserver : RendererObserver<SpriteRenderer, SpriteRendererService>
    {
        protected override void Read(INetworkConnection connection, BinaryReader message, byte changeType)
        {
            base.Read(connection, message, changeType);

            if (SpriteRendererBroadcaster.HasFlag(changeType, SpriteRendererBroadcaster.SpriteRendererChangeType.Sprite))
            {
                AssetId spriteId = message.ReadAssetId();
                Renderer.sprite = ImageService.Instance.GetSprite(spriteId);
            }

            if (SpriteRendererBroadcaster.HasFlag(changeType, SpriteRendererBroadcaster.SpriteRendererChangeType.Properties))
            {
                Renderer.adaptiveModeThreshold = message.ReadSingle();
                Renderer.color = message.ReadColor();
                Renderer.drawMode = (SpriteDrawMode)message.ReadByte();
                Renderer.flipX = message.ReadBoolean();
                Renderer.flipY = message.ReadBoolean();
                Renderer.maskInteraction = (SpriteMaskInteraction)message.ReadByte();
                Renderer.size = message.ReadVector2();
                Renderer.tileMode = (SpriteTileMode)message.ReadByte();
            }
        }
    }
}
