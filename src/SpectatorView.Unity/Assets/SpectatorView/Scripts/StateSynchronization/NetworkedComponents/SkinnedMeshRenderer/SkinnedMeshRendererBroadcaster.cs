// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class SkinnedMeshRendererBroadcaster : RendererBroadcaster<SkinnedMeshRenderer, SkinnedMeshRendererService>
    {
        public static class SkinnedMeshRendererChangeType
        {
            public const byte Bones = 0x8;
            public const byte Mesh = 0x10;
        }

        public bool BonesReady { get; private set;}

        public AssetId NetworkAssetId
        {
            get { return AssetService.Instance.GetMeshId(Renderer.sharedMesh); }
        }

        protected override bool IsRendererEnabled
        {
            get { return base.IsRendererEnabled && this.BonesReady; }
        }

        protected override byte InitialChangeType
        {
            get
            {
                return ChangeType.Materials | ChangeType.Enabled | SkinnedMeshRendererChangeType.Mesh;
            }
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            base.SendCompleteChanges(connections);

            TrySendBones(connections);
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, byte changeFlags)
        {
            base.SendDeltaChanges(connections, changeFlags);

            if (!BonesReady)
            {
                TrySendBones(connections);
            }
        }

        private bool TrySendBones(IEnumerable<INetworkConnection> connections)
        {
            BonesReady = false;
            Transform[] bones = Renderer.bones;

            //Make sure we have transforms ready for all our bones
            foreach (var b in bones)
            {
                if (b.GetComponent<TransformBroadcaster>() == null)
                    return false;

            }
            BonesReady = true;
            SendDeltaChanges(connections, SkinnedMeshRendererChangeType.Bones);
            return true;
        }

        protected override void WriteRenderer(BinaryWriter message, byte changeType)
        {
            if (HasFlag(changeType, SkinnedMeshRendererChangeType.Mesh))
            {
                message.Write(NetworkAssetId);
            }

            base.WriteRenderer(message, changeType);

            if (HasFlag(changeType, SkinnedMeshRendererChangeType.Bones))
            {
                Transform[] bones = Renderer.bones;
                int numBones = bones.Length;
                message.Write((UInt16)numBones);
                for (int i = 0; i < numBones; i++)
                {
                    TransformBroadcaster rootBoneTransform = bones[i].GetComponent<TransformBroadcaster>();
                    message.Write(rootBoneTransform.Id);
                }
            }
        }
    }
}
