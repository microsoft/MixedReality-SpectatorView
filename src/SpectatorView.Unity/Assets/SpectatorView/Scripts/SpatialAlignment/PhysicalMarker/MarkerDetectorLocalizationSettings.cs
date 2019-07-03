// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MarkerDetectorLocalizationSettings : ISpatialLocalizationSettings
#if UNITY_EDITOR
        , IEditableSpatialLocalizationSettings
#endif
    {
        public int MarkerID { get; set; }
        public float MarkerSize { get; set; } = 0.1f;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(MarkerID);
            writer.Write(MarkerSize);
        }

        public static bool TryDeserialize(BinaryReader reader, out MarkerDetectorLocalizationSettings settings)
        {
            try
            {
                settings = new MarkerDetectorLocalizationSettings
                {
                    MarkerID = reader.ReadInt32(),
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
            private MarkerDetectorLocalizationSettings settings;

            public Editor(MarkerDetectorLocalizationSettings settings)
            {
                this.settings = settings;
            }

            public override void OnGUI(Rect rect)
            {
                settings.MarkerID = EditorGUILayout.IntField("Marker ID", settings.MarkerID);
                settings.MarkerSize = EditorGUILayout.FloatField("Marker Size (m)", settings.MarkerSize);
            }
        }
#endif
    }
}