// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    public static class StateSynchronizationMenuItems
    {
        private const string ResourcesDirectoryName = "Resources";
        private static IEqualityComparer<IAssetCacheUpdater> assetTypeComparer = new AssetCacheTypeEqualityComparer();

        private class AssetCacheTypeEqualityComparer : IEqualityComparer<IAssetCacheUpdater>
        {
            public bool Equals(IAssetCacheUpdater x, IAssetCacheUpdater y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }

                return x.GetType().Equals(y.GetType());
            }

            public int GetHashCode(IAssetCacheUpdater obj)
            {
                return obj.GetType().GetHashCode();
            }
        }

        private static IEnumerable<IAssetCacheUpdater> GetAllAssetCaches()
        {
            var assetCaches = AssetCache.EnumerateAllComponentsInScenesAndPrefabs<IAssetCacheUpdater>();
            return assetCaches.Distinct(assetTypeComparer);
        }

        [MenuItem("Spectator View/Update All Asset Caches", priority = 100)]
        public static void UpdateAllAssetCaches()
        {
            bool assetCacheFound = false;

            foreach (IAssetCacheUpdater assetCache in GetAllAssetCaches())
            {
                Debug.Log($"Updating asset cache {assetCache.GetType().Name}...");
                assetCache.UpdateAssetCache();
                assetCacheFound = true;
            }

            if (!assetCacheFound)
            {
                Debug.LogWarning("No asset caches were found in the project. Unable to update asset caches.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Asset caches updated.");
        }

        [MenuItem("Spectator View/Clear All Asset Caches", priority = 101)]
        public static void ClearAllAssetCaches()
        {
            bool assetCacheFound = false;

            foreach (IAssetCacheUpdater assetCache in GetAllAssetCaches())
            {
                Debug.Log($"Clearing asset cache {assetCache.GetType().Name}...");
                assetCache.ClearAssetCache();
                assetCacheFound = true;
            }

            if (!assetCacheFound)
            {
                Debug.LogWarning("No asset caches were found in the project. Unable to clear asset caches.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Asset caches cleared.");
        }

        [MenuItem("Spectator View/Generate Asset Bundles", priority = 102)]
        public static void GenerateAssetBundles()
        {
            string iOSDirectory = Application.dataPath + $"/{AssetCache.assetCacheDirectory}/iOS/{ResourcesDirectoryName}";
            string androidDirectory = Application.dataPath + $"/{AssetCache.assetCacheDirectory}/Android/{ResourcesDirectoryName}";
            string wsaDirectory = Application.dataPath + $"/{AssetCache.assetCacheDirectory}/WSA/{ResourcesDirectoryName}";

            Debug.Log(iOSDirectory);

            Directory.CreateDirectory(iOSDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(androidDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(wsaDirectory.Replace('/', Path.DirectorySeparatorChar));

            List<string> builtDirectories = new List<string>();

#if UNITY_EDITOR_OSX
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            BuildPipeline.BuildAssetBundles(iOSDirectory, BuildAssetBundleOptions.None, BuildTarget.iOS);
            builtDirectories.Add(iOSDirectory);
#elif UNITY_EDITOR_WIN
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildPipeline.BuildAssetBundles(androidDirectory, BuildAssetBundleOptions.None, BuildTarget.Android);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            BuildPipeline.BuildAssetBundles(wsaDirectory, BuildAssetBundleOptions.None, BuildTarget.WSAPlayer);

            builtDirectories.Add(androidDirectory);
            builtDirectories.Add(wsaDirectory);
#endif

            foreach (var directory in builtDirectories)
            {
                string assetPath = $"{directory}/spectatorview".Replace('/', Path.DirectorySeparatorChar);
                string resourcePath = $"{directory}/{ResourcesDirectoryName}".Replace('/', Path.DirectorySeparatorChar);

                File.Delete(resourcePath);
                File.Delete($"{resourcePath}.manifest");
                File.Delete($"{assetPath}.bytes");
                File.Delete($"{assetPath}.manifest.bytes");

                File.Move(assetPath, $"{assetPath}.bytes");
                File.Move($"{assetPath}.manifest", $"{assetPath}.manifest.bytes");
            }
        }

        [MenuItem("Spectator View/Edit Global Performance Parameters", priority = 200)]
        private static void EditGlobalPerformanceParameters()
        {
            GameObject prefab = Resources.Load<GameObject>(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName);
            if (prefab == null)
            {
                GameObject hierarchyPrefab = new GameObject(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName);
                hierarchyPrefab.AddComponent<DefaultStateSynchronizationPerformanceParameters>();

                AssetCache.EnsureAssetDirectoryExists();
                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, AssetCache.GetAssetPath(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName, ".prefab"));
                Object.DestroyImmediate(hierarchyPrefab);
            }

            AssetDatabase.OpenAsset(prefab);
        }

        [MenuItem("Spectator View/Edit Custom Network Services", priority = 201)]
        private static void EditCustomShaderProperties()
        {
            GameObject prefab = Resources.Load<GameObject>(StateSynchronizationSceneManager.CustomBroadcasterServicesPrefabName);
            if (prefab == null)
            {
                GameObject hierarchyPrefab = new GameObject(StateSynchronizationSceneManager.CustomBroadcasterServicesPrefabName);

                AssetCache.EnsureAssetDirectoryExists();
                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, AssetCache.GetAssetPath(StateSynchronizationSceneManager.CustomBroadcasterServicesPrefabName, ".prefab"));
                Object.DestroyImmediate(hierarchyPrefab);
            }

            AssetDatabase.OpenAsset(prefab);
        }

        [MenuItem("Spectator View/Edit Settings", priority = 202)]
        private static void EditCustomSettingsProperties()
        {
            GameObject prefab = Resources.Load<GameObject>(SpectatorView.SettingsPrefabName);
            GameObject hierarchyPrefab = null;
            if (prefab == null)
            {
                hierarchyPrefab = new GameObject(SpectatorView.SettingsPrefabName);
                hierarchyPrefab.AddComponent<BroadcasterSettings>();
                hierarchyPrefab.AddComponent<SpatialLocalizationInitializationSettings>();
                hierarchyPrefab.AddComponent<MobileRecordingSettings>();
                hierarchyPrefab.AddComponent<NetworkConfigurationSettings>();

                AssetCache.EnsureAssetDirectoryExists();
                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, AssetCache.GetAssetPath(SpectatorView.SettingsPrefabName, ".prefab"));
                Object.DestroyImmediate(hierarchyPrefab);
            }
            else
            {
                GameObject editablePrefab = PrefabUtility.LoadPrefabContents(AssetCache.GetAssetPath(SpectatorView.SettingsPrefabName, ".prefab"));
                EnsureComponent<BroadcasterSettings>(editablePrefab);
                EnsureComponent<SpatialLocalizationInitializationSettings>(editablePrefab);
                EnsureComponent<MobileRecordingSettings>(editablePrefab);
                EnsureComponent<NetworkConfigurationSettings>(editablePrefab);
                PrefabUtility.SaveAsPrefabAsset(editablePrefab, AssetCache.GetAssetPath(SpectatorView.SettingsPrefabName, ".prefab"));
                PrefabUtility.UnloadPrefabContents(editablePrefab);
            }

            AssetDatabase.OpenAsset(prefab);
        }

        private static void EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
        }
    }
}
