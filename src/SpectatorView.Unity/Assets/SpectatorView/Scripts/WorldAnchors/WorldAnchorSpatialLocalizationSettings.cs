using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.WorldAnchors
{
    public enum WorldAnchorLocalizationMode
    {
        LocateExistingAnchor,
        CreateAnchorAtWorldTransform
    }

    public class WorldAnchorSpatialLocalizationSettings : ISpatialLocalizationSettings
    {
        public WorldAnchorLocalizationMode Mode { get; set; }

        public Vector3 AnchorPosition { get; set; }

        public Quaternion AnchorRotation { get; set; }

        public string AnchorId { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Mode);
            writer.Write(AnchorId);
            writer.Write(AnchorPosition);
            writer.Write(AnchorRotation);
        }

        public static bool TryDeserialize(BinaryReader reader, out WorldAnchorSpatialLocalizationSettings settings)
        {
            try
            {
                settings = new WorldAnchorSpatialLocalizationSettings
                {
                    Mode = (WorldAnchorLocalizationMode)reader.ReadByte(),
                    AnchorId = reader.ReadString(),
                    AnchorPosition = reader.ReadVector3(),
                    AnchorRotation = reader.ReadQuaternion()
                };
                return true;
            }
            catch
            {
                settings = null;
                return false;
            }
        }
    }
}