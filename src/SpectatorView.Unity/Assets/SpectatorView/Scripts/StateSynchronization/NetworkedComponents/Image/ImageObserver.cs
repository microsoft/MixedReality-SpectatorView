// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class ImageObserver : ComponentObserver<Image>
    {
        public override void Read(INetworkConnection connection, BinaryReader message)
        {
            ImageBroadcaster.ChangeType changeType = (ImageBroadcaster.ChangeType)message.ReadByte();

            if (ImageBroadcaster.HasFlag(changeType, ImageBroadcaster.ChangeType.Data))
            {
                attachedComponent.overrideSprite = ImageService.Instance.GetSprite(message.ReadAssetId());
                attachedComponent.sprite = ImageService.Instance.GetSprite(message.ReadAssetId());
                attachedComponent.fillAmount = message.ReadSingle();
                attachedComponent.color = message.ReadColor();

                attachedComponent.alphaHitTestMinimumThreshold = message.ReadSingle();
                attachedComponent.fillOrigin = message.ReadInt32();
                attachedComponent.fillClockwise = message.ReadBoolean();
                attachedComponent.fillMethod = (Image.FillMethod)message.ReadByte();
                attachedComponent.fillCenter = message.ReadBoolean();
                attachedComponent.preserveAspect = message.ReadBoolean();
                attachedComponent.type = (Image.Type)message.ReadByte();
                attachedComponent.enabled = message.ReadBoolean();
            }
            if (ImageBroadcaster.HasFlag(changeType, ImageBroadcaster.ChangeType.Materials))
            {
                var materials = MaterialPropertyAsset.ReadMaterials(message, null);
                if (materials != null &&
                    materials.Length > 0)
                {
                    attachedComponent.material = materials[0];
                }
            }
            if (ImageBroadcaster.HasFlag(changeType, ImageBroadcaster.ChangeType.MaterialProperty))
            {
                int materialIndex = message.ReadInt32();
                MaterialPropertyAsset.Read(message, new Material[] { attachedComponent.material }, materialIndex);
            }
        }

        public void LerpRead(BinaryReader message, float lerpVal)
        {
            if (attachedComponent == null)
                return;

            ImageBroadcaster.ChangeType changeType = (ImageBroadcaster.ChangeType)message.ReadByte();

            //Only lerp messages with data changes on its own
            if (changeType == ImageBroadcaster.ChangeType.Data)
            {
                attachedComponent.overrideSprite = ImageService.Instance.GetSprite(message.ReadAssetId());
                attachedComponent.sprite = ImageService.Instance.GetSprite(message.ReadAssetId());
                attachedComponent.fillAmount = Mathf.Lerp(attachedComponent.fillAmount, message.ReadSingle(), lerpVal);
                attachedComponent.color = Color.Lerp(attachedComponent.color, message.ReadColor(), lerpVal);

                //Dont read and lerp the rest of the data
                return;
            }
        }

    }
}