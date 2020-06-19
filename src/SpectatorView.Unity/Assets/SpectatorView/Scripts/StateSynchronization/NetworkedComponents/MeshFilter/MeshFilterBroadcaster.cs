// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class MeshFilterBroadcaster : MeshRendererBroadcaster<MeshFilterService>
    {
        // A unique integer value used to check that mesh data is correctly written and read.
        // TODO: After some time, if this is working reliably, this check could be removed.
        public const int CheckValue = 123454321;

        public static class MeshFilterChangeType
        {
            public const byte Mesh = 0x8;
        }

        private MeshFilter meshFilter;
        private AssetId assetId;

        protected override byte InitialChangeType
        {
            get { return (byte)(base.InitialChangeType | MeshFilterChangeType.Mesh); }
        }

        protected override void WriteRenderer(BinaryWriter message, byte changeType)
        {
            if (HasFlag(changeType, MeshFilterChangeType.Mesh))
            {
                message.Write(assetId);

                if (assetId == AssetId.Empty)
                {
                    // This is a dynamic object that wasn't created from an asset.
                    bool hasDynamicMesh = meshFilter.sharedMesh != null;
                    message.Write(hasDynamicMesh);
                    if (hasDynamicMesh)
                    {
                        WriteDynamicMesh(message, meshFilter.sharedMesh);
                    }
                }
            }

            base.WriteRenderer(message, changeType);
        }

        private void WriteDynamicMesh(BinaryWriter message, Mesh mesh)
        {
            message.Write(CheckValue);
            message.Write(mesh.subMeshCount);
            message.Write(mesh.vertices);
            message.Write(mesh.uv);
            message.Write(mesh.uv2);
            message.Write(mesh.uv3);
            message.Write(mesh.uv4);
            message.Write(mesh.uv5);
            message.Write(mesh.uv6);
            message.Write(mesh.uv7);
            message.Write(mesh.uv8);
            message.Write(mesh.colors);
            message.Write(mesh.triangles);
            message.Write(CheckValue);

            Debug.Log($"MeshFilterBroadcaster.WriteDynamicMesh: {gameObject.name} written with {mesh.subMeshCount} subMeshCount, {mesh.vertices?.Length} vertices, {mesh.triangles?.Length} triangles");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            meshFilter = GetComponent<MeshFilter>();
            assetId = AssetService.Instance.GetMeshId(meshFilter.sharedMesh);
            if (assetId == AssetId.Empty)
            {
                Debug.LogError("Could not find the Mesh asset for GameObject " + this.gameObject.name + ". Check the NetworkAssetCache and ensure that you're not modifying the mesh by accessing the MeshFilter.mesh property");
            }
        }
    }
}