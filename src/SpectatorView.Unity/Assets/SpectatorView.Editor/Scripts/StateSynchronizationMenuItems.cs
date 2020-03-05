// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Microsoft.MixedReality.SpectatorView.Editor
{
    public static class StateSynchronizationMenuItems
    {
        private static IEqualityComparer<IAssetCache> assetTypeComparer = new AssetCacheTypeEqualityComparer();

        private const string disableSpectatorViewPreBuild = "Spectator View/Disable Spectator View Pre-build steps";
        public static bool DisablePreBuildSteps
        {
            get
            {
                return PlayerPrefs.GetInt(disableSpectatorViewPreBuild, 0) > 0;
            }
            private set
            {
                PlayerPrefs.SetInt(disableSpectatorViewPreBuild, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private const string disableUpdatingMenuItem = "Spectator View/Disable updating asset caches when building";
        public static bool DisableUpdatingAssetCaches
        {
            get
            {
                return PlayerPrefs.GetInt(disableUpdatingMenuItem, 0) > 0;
            }
            private set
            {
                PlayerPrefs.SetInt(disableUpdatingMenuItem, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private class AssetCacheTypeEqualityComparer : IEqualityComparer<IAssetCache>
        {
            public bool Equals(IAssetCache x, IAssetCache y)
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

            public int GetHashCode(IAssetCache obj)
            {
                return obj.GetType().GetHashCode();
            }
        }

        private static IEnumerable<IAssetCache> GetAllAssetCaches()
        {
            var assetCaches = AssetCache.EnumerateAllComponentsInScenesAndPrefabs<IAssetCache>();
            return assetCaches.Distinct(assetTypeComparer);
        }

        [MenuItem(disableSpectatorViewPreBuild, priority = 100)]
        public static void DisableSpectatorViewPreBuild()
        {
            DisablePreBuildSteps = !DisablePreBuildSteps;
            Menu.SetChecked(disableSpectatorViewPreBuild, DisablePreBuildSteps);
        }

        [MenuItem(disableSpectatorViewPreBuild, true, priority = 100)]
        public static bool DisableSpectatorViewPreBuild_Validate()
        {
            Menu.SetChecked(disableSpectatorViewPreBuild, DisablePreBuildSteps);
            return true;
        }

        [MenuItem(disableUpdatingMenuItem, priority = 101)]
        public static void DisableUpdatingAssetCachesWhenBuilding()
        {
            DisableUpdatingAssetCaches = !DisableUpdatingAssetCaches;
            Menu.SetChecked(disableUpdatingMenuItem, DisableUpdatingAssetCaches);
        }

        [MenuItem(disableUpdatingMenuItem, true, priority = 101)]
        public static bool DisableUpdatingAssetCachesWhenBuilding_Validate()
        {
            Menu.SetChecked(disableUpdatingMenuItem, DisableUpdatingAssetCaches);
            return true;
        }

        [MenuItem("Spectator View/Update All Asset Caches", priority = 102)]
        public static void UpdateAllAssetCaches()
        {
            bool assetCacheFound = false;

            IEnumerable<IAssetCache> assetCaches = GetAllAssetCaches();
            int numCaches = assetCaches.Count();
            for (int i = 0; i < numCaches; i++)
            {
                IAssetCache assetCache = assetCaches.ElementAt(i);
                EditorUtility.DisplayProgressBar($"Updating {numCaches} Asset Caches...", $"Updating the {assetCache.GetType().Name}'s Asset Caches.", i / (float)numCaches);
                assetCache.UpdateAssetCache();
                assetCache.SaveAssets();
                assetCacheFound = true;
            }
            EditorUtility.ClearProgressBar();

            if (!assetCacheFound)
            {
                Debug.LogWarning("No asset caches were found in the project. Unable to update asset caches.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Asset caches updated.");
        }

        [MenuItem("Spectator View/Clear All Asset Caches", priority = 103)]
        public static void ClearAllAssetCaches()
        {
            bool assetCacheFound = false;

            foreach (IAssetCache assetCache in GetAllAssetCaches())
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

        [MenuItem("Spectator View/Edit Global Performance Parameters", priority = 200)]
        internal static void EditGlobalPerformanceParameters()
        {
            GameObject prefab = Resources.Load<GameObject>(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName);
            if (prefab == null)
            {
                GameObject hierarchyPrefab = new GameObject(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName);
                hierarchyPrefab.AddComponent<DefaultStateSynchronizationPerformanceParameters>();

                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, SpectatorViewSettings.GetSettingsPath(StateSynchronizationSceneManager.DefaultStateSynchronizationPerformanceParametersPrefabName, ".prefab"));
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

                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, SpectatorViewSettings.GetSettingsPath(StateSynchronizationSceneManager.CustomBroadcasterServicesPrefabName, ".prefab"));
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

                prefab = PrefabUtility.SaveAsPrefabAsset(hierarchyPrefab, SpectatorViewSettings.GetSettingsPath(SpectatorView.SettingsPrefabName, ".prefab"));
                Object.DestroyImmediate(hierarchyPrefab);
            }
            else
            {
                GameObject editablePrefab = PrefabUtility.LoadPrefabContents(SpectatorViewSettings.GetSettingsPath(SpectatorView.SettingsPrefabName, ".prefab"));
                EnsureComponent<BroadcasterSettings>(editablePrefab);
                EnsureComponent<SpatialLocalizationInitializationSettings>(editablePrefab);
                EnsureComponent<MobileRecordingSettings>(editablePrefab);
                EnsureComponent<NetworkConfigurationSettings>(editablePrefab);
                PrefabUtility.SaveAsPrefabAsset(editablePrefab, SpectatorViewSettings.GetSettingsPath(SpectatorView.SettingsPrefabName, ".prefab"));
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
