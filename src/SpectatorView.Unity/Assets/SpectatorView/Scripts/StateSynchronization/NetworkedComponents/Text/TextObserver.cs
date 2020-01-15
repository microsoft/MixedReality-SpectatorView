// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class TextObserver : ComponentObserver<Text>
    {
        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            TextBroadcaster.ChangeType changeType = (TextBroadcaster.ChangeType)message.ReadByte();

            if (TextBroadcaster.HasFlag(changeType, TextBroadcaster.ChangeType.Text))
            {
                attachedComponent.text = message.ReadString();
            }
            if (TextBroadcaster.HasFlag(changeType, TextBroadcaster.ChangeType.FontAndPlacement))
            {
                attachedComponent.alignment = (TextAnchor)message.ReadByte();
                attachedComponent.color = message.ReadColor();
                attachedComponent.fontSize = message.ReadInt32();
                attachedComponent.fontStyle = (FontStyle)message.ReadByte();
                attachedComponent.lineSpacing = message.ReadSingle();
                attachedComponent.horizontalOverflow = (HorizontalWrapMode)message.ReadByte();
                attachedComponent.verticalOverflow = (VerticalWrapMode)message.ReadByte();
                attachedComponent.font = TextService.Instance.GetFont(message.ReadAssetId());
            }
            if (TextBroadcaster.HasFlag(changeType, TextBroadcaster.ChangeType.Materials))
            {
                var materials = MaterialPropertyAsset.ReadMaterials(message, null);
                if (materials != null &&
                    materials.Length > 0)
                {
                    attachedComponent.material = materials[0];
                }
            }
            if (TextBroadcaster.HasFlag(changeType, TextBroadcaster.ChangeType.MaterialProperty))
            {
                int materialIndex = message.ReadInt32();
                MaterialPropertyAsset.Read(message, new Material[] { attachedComponent.material }, materialIndex);
            }
        }
    }
}