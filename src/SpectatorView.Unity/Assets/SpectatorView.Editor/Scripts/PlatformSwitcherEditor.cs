// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
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
                EditorUserBuildSettings.wsaArchitecture = "x86";
                EditorUserBuildSettings.wsaSubtarget = WSASubtarget.HoloLens;
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);

                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClient, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.WebCam, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.Microphone, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PicturesLibrary, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.VideosLibrary, true);
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, true);
                PlayerSettings.WSA.SetTargetDeviceFamily(PlayerSettings.WSATargetFamily.Holographic, true);
            }

            // Editor button for Android platform and functionality
            if (GUILayout.Button("Android", GUILayout.Height(_buttonHeight)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait; // Currently needed based on Marker Visual logic

                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.forceInternetPermission = true;
                PlayerSettings.Android.forceSDCardPermission = true;

                DeployAndValidateAndroidManifest();
            }

            // Editor button for iOS platform and functionality
            if (GUILayout.Button("iOS", GUILayout.Height(_buttonHeight)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait; // Currently needed based on Marker Visual logic

                PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // Set Architecture to ARM64
                PlayerSettings.iOS.targetOSVersionString = "11.0";
                // TODO: figure out how to programmatically check the "Requires ARKit Support" box (corresponding to "iOSRequireARKit" in ProjectSettings.asset).
                PlayerSettings.iOS.cameraUsageDescription = "Camera required for AR Foundation";
            }

            GUILayout.EndVertical();
        }

        private void DeployAndValidateAndroidManifest()
        {
            var assetsDirectoryPath = Application.dataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string sourceManifestPath = Path.Combine(assetsDirectoryPath, "MixedReality-SpectatorView", "SpectatorView", "Scripts", "ScreenRecording", "Plugins", "Android", "AndroidManifestTemplate.xml");

            string destManifestDirectoryPath = Path.Combine(assetsDirectoryPath, "Plugins", "Android");
            string destManifestPath = Path.Combine(destManifestDirectoryPath, "AndroidManifest.xml");

            try
            {
                if (File.Exists(destManifestPath))
                {
                    Debug.Log($"Android manifest \"{destManifestPath}\" already exists. Not copying from \"{sourceManifestPath}\".", this);
                }
                else
                {
                    Debug.Log($"Copying android manifest \"{sourceManifestPath}\" to \"{destManifestPath}\".", this);

                    Directory.CreateDirectory(destManifestDirectoryPath);
                    File.Copy(sourceManifestPath, destManifestPath);
                }

                var manifest = XElement.Load(destManifestPath);
                var activities = manifest.XPathSelectElements("application/activity").ToArray();

                if (activities.Length != 1)
                {
                    throw new System.Exception($"Expected 1 application activity, but got {activities.Length}.");
                }

                var nameAttributes = activities[0].Attributes(XName.Get("name", "http://schemas.android.com/apk/res/android")).ToArray();

                if (nameAttributes.Length != 1)
                {
                    throw new System.Exception($"Expected 1 name attribute on the application activity, but got {nameAttributes.Length}.");
                }

                var expectedNameAttribute = "Microsoft.MixedReality.SpectatorView.Unity.ScreenRecorderActivity";

                if (nameAttributes[0].Value != expectedNameAttribute)
                {
                    throw new System.Exception($"Expected name attribute on the application activity to be \"{expectedNameAttribute}\", but got \"{nameAttributes[0].Value}\".");
                }

                // It's valid!
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Valid android manifest not found at \"{destManifestPath}\". Error: {ex}", this);
            }
        }
    }
}
