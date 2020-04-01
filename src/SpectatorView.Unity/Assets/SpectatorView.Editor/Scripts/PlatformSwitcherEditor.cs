// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    /// <summary>
    /// Defines functionality for switching platforms in the Unity editor
    /// </summary>
    [CustomEditor(typeof(PlatformSwitcher))]
    public class PlatformSwitcherEditor : UnityEditor.Editor
    {
        private readonly float _buttonHeight = 30;

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical();

            // Editor button for HoloLens platform and functionality
            if (GUILayout.Button("HoloLens", GUILayout.Height(_buttonHeight)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            }

            // Editor button for Android platform and functionality
            if (GUILayout.Button("Android", GUILayout.Height(_buttonHeight)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Editor button for iOS platform and functionality
            if (GUILayout.Button("iOS", GUILayout.Height(_buttonHeight)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            }

            GUILayout.EndVertical();
        }
    }
}
