// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ArUcoThreeMarkerLocalizationSettings : ISpatialLocalizationSettings, IEditableSpatialLocalizationSettings
    {
        public int TopMarkerID { get; set; } = 0;
        public int MiddleMarkerID { get; set; } = 1;
        public int BottomMarkerID { get; set; } = 2;
        public float MarkerSize { get; set; } = 0.1f;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TopMarkerID);
            writer.Write(MiddleMarkerID);
            writer.Write(BottomMarkerID);
            writer.Write(MarkerSize);
        }

        public static bool TryDeserialize(BinaryReader reader, out ArUcoThreeMarkerLocalizationSettings settings)
        {
            try
            {
                settings = new ArUcoThreeMarkerLocalizationSettings
                {
                    TopMarkerID = reader.ReadInt32(),
                    MiddleMarkerID = reader.ReadInt32(),
                    BottomMarkerID = reader.ReadInt32(),
                    MarkerSize = reader.ReadSingle(),
                };
                return true;
            }
            catch
            {
                settings = null;
                return false;
            }
        }

#if UNITY_EDITOR
        public SpatialLocalizationSettingsEditor CreateEditor()
        {
            return new Editor(this);
        }

        private class Editor : SpatialLocalizationSettingsEditor
        {
            private ArUcoThreeMarkerLocalizationSettings settings;

            public Editor(ArUcoThreeMarkerLocalizationSettings settings)
            {
                this.settings = settings;
            }

            public override void OnGUI(Rect rect)
            {
                settings.TopMarkerID = EditorGUILayout.IntField("Top Marker ID", settings.TopMarkerID);
                settings.MiddleMarkerID = EditorGUILayout.IntField("Middle Marker ID", settings.MiddleMarkerID);
                settings.BottomMarkerID = EditorGUILayout.IntField("Bottom Marker ID", settings.BottomMarkerID);
                settings.MarkerSize = EditorGUILayout.FloatField("Marker Size (m)", settings.MarkerSize);
            }
        }
#endif
    }
}