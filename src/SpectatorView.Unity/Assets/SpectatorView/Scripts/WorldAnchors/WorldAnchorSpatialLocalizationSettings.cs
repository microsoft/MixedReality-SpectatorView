using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.WorldAnchors
{
    /// <summary>
    /// Specifies how to create a shared coordinate based on a WorldAnchor.
    /// </summary>
    public enum WorldAnchorLocalizationMode
    {
        /// <summary>
        /// Specifies a request to create a spatial coordinate from a WorldAnchor stored
        /// in the device's WorldAnchorStore.
        /// </summary>
        LocateExistingAnchor,

        /// <summary>
        /// Specifies a request to create a new spatial coordinate at a specific position
        /// and rotation and to store that spatial coordinate using a WorldAnchor
        /// stored in the device's WorldAnchorStore.
        /// </summary>
        CreateAnchorAtWorldTransform
    }

    /// <summary>
    /// Settings for localizing using the <see cref="WorldAnchorSpatialLocalizer"/>
    /// </summary>
    public class WorldAnchorSpatialLocalizationSettings : ISpatialLocalizationSettings
    {
        /// <summary>
        /// The mode used to create a spatial coordinate.
        /// </summary>
        public WorldAnchorLocalizationMode Mode { get; set; }

        /// <summary>
        /// When using the <see cref="WorldAnchorLocalizationMode.CreateAnchorAtWorldTransform"/> mode,
        /// specifies the world position at which the WorldAnchor should be created.
        /// </summary>
        public Vector3 AnchorPosition { get; set; }

        /// <summary>
        /// When using the <see cref="WorldAnchorLocalizationMode.CreateAnchorAtWorldTransform"/> mode,
        /// specifies the world rotation at which the WorldAnchor should be created.
        /// </summary>
        public Quaternion AnchorRotation { get; set; }

        /// <summary>
        /// Specifies the ID of the anchor, used to persist and restore the anchor across sessions.
        /// </summary>
        public string AnchorId { get; set; }

        /// <inheritdoc />
        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Mode);
            writer.Write(AnchorId);
            writer.Write(AnchorPosition);
            writer.Write(AnchorRotation);
        }

        /// <summary>
        /// Tries to deserialize settings from a stream.
        /// </summary>
        /// <param name="reader">The reader to deserialize settings from.</param>
        /// <param name="settings">The deserialized settings.</param>
        /// <returns>True if the settings were successfully deserialized, otherwise false.</returns>
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