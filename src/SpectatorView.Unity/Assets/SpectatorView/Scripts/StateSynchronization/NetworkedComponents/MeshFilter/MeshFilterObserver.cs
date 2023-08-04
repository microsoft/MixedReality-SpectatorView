// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class MeshFilterObserver : MeshRendererObserver<MeshFilterService>
    {
        protected override void EnsureRenderer(BinaryReader message, byte changeType)
        {
            if (MeshFilterBroadcaster.HasFlag(changeType, MeshFilterBroadcaster.MeshFilterChangeType.Mesh))
            {
                AssetId assetId = message.ReadAssetId();
                if (assetId != AssetId.Empty)
                {
                    AssetService.Instance.AttachMeshFilter(this.gameObject, assetId);
                }
                else
                {
                    bool hasDynamicMesh = message.ReadBoolean();
                    if (hasDynamicMesh)
                    {
                        Mesh mesh = ReadDynamicMesh(message);
                        AssetService.Instance.AttachDynamicMeshFilter(this.gameObject, assetId, mesh);
                    }
                }
            }

            base.EnsureRenderer(message, changeType);
        }

        private Mesh ReadDynamicMesh(BinaryReader message)
        {
            int checkValue = message.ReadInt32();
            if (checkValue != MeshFilterBroadcaster.CheckValue)
            {
                Debug.LogError($"MeshFilterObserver.ReadDynamicMesh: {gameObject.name} initial checkValue mismatch! All subsequent spectator view data will read incorrectly!");
            }

            Mesh mesh = new Mesh();
            mesh.subMeshCount = message.ReadInt32();
            mesh.vertices = message.ReadVector3Array();
            mesh.uv = message.ReadVector2Array();
            mesh.uv2 = message.ReadVector2Array();
            mesh.uv3 = message.ReadVector2Array();
            mesh.uv4 = message.ReadVector2Array();
            mesh.uv5 = message.ReadVector2Array();
            mesh.uv6 = message.ReadVector2Array();
            mesh.uv7 = message.ReadVector2Array();
            mesh.uv8 = message.ReadVector2Array();
            mesh.colors = message.ReadColorArray();
            mesh.triangles = message.ReadInt32Array();
            mesh.RecalculateNormals();

            checkValue = message.ReadInt32();
            if (checkValue != MeshFilterBroadcaster.CheckValue)
            {
                Debug.LogError($"MeshFilterObserver.ReadDynamicMesh: {gameObject.name} final checkValue mismatch! All subsequent spectator view data will read incorrectly!");
            }

            Debug.Log($"MeshFilterObserver.ReadDynamicMesh: {gameObject.name} read with {mesh.subMeshCount} subMeshCount, {mesh.vertices?.Length} vertices, {mesh.triangles?.Length} triangles");
            return mesh;
        }
    }
}