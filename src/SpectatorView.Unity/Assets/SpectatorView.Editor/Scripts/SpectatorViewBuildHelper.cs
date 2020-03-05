// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    public class SpectatorViewBuildHelper : IPreprocessBuildWithReport
    {
        public const string ScreenRecorderActivityName = "Microsoft.MixedReality.SpectatorView.Unity.ScreenRecorderActivity";

        public int callbackOrder => 0; // Execute this first

        private static string androidPluginsDirectory { get; } = Path.Combine(Application.dataPath, "Plugins", "Android");

        private const string YesDialogOption = "Yes";
        private const string NoDialogOption = "No";
        private const string SpectatorViewBuildToolsTitle = "Spectator View Build Tools";
        private const string AndroidManifestDialogMessage = "An existing AndroidManifest.xml was detected with conflicting content.\n\nShould Spectator View build tools overwrite your AndroidManifest.xml?";
        private const string AndroidGradleDialogMessage = "An existing mainTemplate.gradle file was detected with conflicting content.\n\nShould Spectator View build tools overwrite your mainTemplate.gradle file?";
        private const string AndroidGradleBackupDialogMessage = "An existing mainTemplate.gradle.backup file was detected.\n\nShould Spectator View build tools delete your mainTemplate.gradle.backup file? Note: choosing not deleting this file may result in the Unity Editor generating IOExceptions.";

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!StateSynchronizationMenuItems.DisablePreBuildSteps)
            {
                RunPreBuildSteps();
            }

            if (!StateSynchronizationMenuItems.DisableUpdatingAssetCaches)
            {
                StateSynchronizationMenuItems.UpdateAllAssetCaches();
            }
        }

        private void RunPreBuildSteps()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
            {
                EditorUserBuildSettings.wsaArchitecture = "x86";
                EditorUserBuildSettings.wsaSubtarget = WSASubtarget.HoloLens;
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
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait; // Currently needed based on Marker Visual logic
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                PlayerSettings.Android.forceInternetPermission = true;
                PlayerSettings.Android.forceSDCardPermission = true;
                PlayerSettings.Android.ARCoreEnabled = false;

                EnsureAndroidPluginsDirectory();
                SetupAndroidManifestFiles();
                SetupAndroidGradleFiles();
                AssetDatabase.Refresh();
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait; // Currently needed based on Marker Visual logic
                PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // Set Architecture to ARM64
                PlayerSettings.iOS.targetOSVersionString = "11.0";
                // TODO: figure out how to programmatically check the "Requires ARKit Support" box (corresponding to "iOSRequireARKit" in ProjectSettings.asset).
                PlayerSettings.iOS.cameraUsageDescription = "Camera required for AR Foundation";
            }
        }

        private void EnsureAndroidPluginsDirectory()
        {
            if (!Directory.Exists(androidPluginsDirectory.ToString()))
            {
                Directory.CreateDirectory(androidPluginsDirectory);
            }
        }

        private void SetupAndroidManifestFiles()
        {
            IEnumerable<string> androidManifestPaths = AssetDatabase.FindAssets("AndroidManifest")
                .Select(assetId => AssetDatabase.GUIDToAssetPath(assetId))
                .Where(assetPath => assetPath.Contains("/SpatialAlignment.ASA/Plugins/Android/"));
            if (androidManifestPaths.Count() != 1)
            {
                Debug.LogError("Located multiple Azure Spatial Anchors AndroidManifest.xml files. Failed to configure Spectator View for Android.");
                return;
            }

            string asaManifestPath = Path.Combine(androidManifestPaths.First());
            var manifest = XElement.Load(asaManifestPath);

            var packageAttributes = manifest.Attributes(XName.Get("package")).ToArray();
            string androidPackageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            Debug.Log($"Setting AndroidManifest package name: {androidPackageName}");
            packageAttributes[0].Value = androidPackageName;

            var activities = manifest.XPathSelectElements("application/activity").ToArray();
            if (activities.Length != 1)
            {
                Debug.LogError($"Expected 1 application activity, but got {activities.Length}.");
                return;
            }

            var nameAttributes = activities[0].Attributes(XName.Get("name", "http://schemas.android.com/apk/res/android")).ToArray();
            if (nameAttributes.Length != 1)
            {
                Debug.LogError($"Expected 1 name attribute on the application activity, but got {nameAttributes.Length}.");
                return;
            }

            if (nameAttributes[0].Value != ScreenRecorderActivityName)
            {
                Debug.Log($"Setting Android activity to be: {ScreenRecorderActivityName}");
                nameAttributes[0].Value = ScreenRecorderActivityName;
            }

            byte[] manifestData;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                manifest.Save(memoryStream);
                memoryStream.Flush();
                manifestData = memoryStream.ToArray();
            }

            string outputManifestFilePath = Path.Combine(androidPluginsDirectory, "AndroidManifest.xml");
            bool saveManifest = false;
            if (File.Exists(outputManifestFilePath))
            {
                byte[] existingManifestFile = File.ReadAllBytes(outputManifestFilePath);
                if (existingManifestFile != null &&
                    !existingManifestFile.SequenceEqual(manifestData) &&
                    EditorUtility.DisplayDialog(SpectatorViewBuildToolsTitle, AndroidManifestDialogMessage, YesDialogOption, NoDialogOption))
                {
                    Debug.Log("User chose to overwrite an existing AndroidManifest.xml.");
                    File.Delete(outputManifestFilePath);
                    saveManifest = true;
                }
                else
                {
                    Debug.Log("Pre-existing AndroidManifest.xml was used.");
                }
            }
            else
            {
                saveManifest = true;
            }

            if (saveManifest)
            {
                File.WriteAllBytes(outputManifestFilePath, manifestData);
                Debug.Log($"Created AndroidManifest file from spectator view content: {outputManifestFilePath}");
            }
        }

        private void SetupAndroidGradleFiles()
        {
            IEnumerable<string> gradleFiles = AssetDatabase.FindAssets("mainTemplate")
                .Select(assetId => AssetDatabase.GUIDToAssetPath(assetId))
                .Where(assetPath => assetPath.Contains("/SpatialAlignment.ASA/Plugins/Android/"));
            if (gradleFiles.Count() != 1)
            {
                Debug.LogError("Located multiple Azure Spatial Anchors mainTemplate.gradle files. Failed to configure Spectator View for Android.");
                return;
            }

            string outputGradleBackupFilePath = Path.Combine(androidPluginsDirectory, "mainTemplate.gradle.backup");
            string outputGradleBackupMetaFilePath = Path.Combine(androidPluginsDirectory, "mainTemplate.gradle.backup.meta");
            if (File.Exists(outputGradleBackupFilePath) &&
                EditorUtility.DisplayDialog(SpectatorViewBuildToolsTitle, AndroidGradleBackupDialogMessage, YesDialogOption, NoDialogOption))
            {
                File.Delete(outputGradleBackupFilePath);
                File.Delete(outputGradleBackupMetaFilePath);
            }

            string outputGradleFilePath = Path.Combine(androidPluginsDirectory, "mainTemplate.gradle");
            bool copyGradle = false;
            if (File.Exists(outputGradleFilePath))
            {
                byte[] newGradle = File.ReadAllBytes(gradleFiles.First());
                byte[] existingGradle = File.ReadAllBytes(outputGradleFilePath);
                if (!newGradle.SequenceEqual(existingGradle) &&
                    EditorUtility.DisplayDialog(SpectatorViewBuildToolsTitle, AndroidGradleDialogMessage, YesDialogOption, NoDialogOption))
                {
                    Debug.Log("User chose to overwrite an existin mainTemplate.gradle file.");
                    File.Delete(outputGradleFilePath);
                    copyGradle = true;
                }
                else
                {
                    Debug.Log("Pre-existing mainTemplate.gradle was used.");
                }
            }
            else
            {
                copyGradle = true;
            }

            if (copyGradle)
            {
                File.Copy(gradleFiles.First(), outputGradleFilePath);
            }
        }
    }
}
