// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#define UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    internal struct NameEntry
    {
        public string Name;
        public AssetId[] Ids;

        public NameEntry(string name, AssetId[] ids)
        {
            this.Name = name;
            this.Ids = ids;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    internal abstract class AssetCache : ScriptableObject
    {
        const string assetCacheDirectory = "Generated.StateSynchronization.AssetCaches";
        protected const string assetsFolderName = "AssetCacheContent";

        public static TAssetCache LoadAssetCache<TAssetCache>()
            where TAssetCache : AssetCache
        {
            using (StateSynchronizationPerformanceMonitor.Instance.IncrementEventDuration(typeof(TAssetCache).Name, "LoadAssetCache"))
            using(StateSynchronizationPerformanceMonitor.Instance.MeasureEventMemoryUsage(typeof(TAssetCache).Name, "LoadingAssets"))
            {
                var cache = Resources.Load<TAssetCache>(typeof(TAssetCache).Name);

                if (BroadcasterSettings.IsInitialized &&
                    BroadcasterSettings.Instance.ForceLoadAllAssetsDuringInitialization)
                {
                    cache.LoadAllAssets();
                }

                return cache;
            }
        }

        public static string GetAssetCachePath(string assetName, string assetExtension)
        {
            return $"Assets/{assetCacheDirectory}/Resources/{assetName}{assetExtension}";
        }

        public static string GetAssetCachesContentPath(string assetName, string assetExtension)
        {
            return $"Assets/{assetCacheDirectory}/Resources/{assetsFolderName}/{assetName}{assetExtension}";
        }

        public static void EnsureAssetDirectoryExists()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder( $"Assets/{assetCacheDirectory}"))
            {
                AssetDatabase.CreateFolder("Assets", $"{assetCacheDirectory}");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/{assetCacheDirectory}/Resources"))
            {
                AssetDatabase.CreateFolder($"Assets/{assetCacheDirectory}", "Resources");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/{assetCacheDirectory}/Resources/{assetsFolderName}"))
            {
                AssetDatabase.CreateFolder($"Assets/{assetCacheDirectory}/Resources", $"{assetsFolderName}");
            }
#endif
        }

        public static TAssetCache GetOrCreateAssetCache<TAssetCache>()
            where TAssetCache : AssetCache
        {
#if UNITY_EDITOR
            string assetPathAndName = GetAssetCachePath(typeof(TAssetCache).Name, ".asset");

            TAssetCache asset = AssetDatabase.LoadAssetAtPath<TAssetCache>(assetPathAndName);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TAssetCache>();

                EnsureAssetDirectoryExists();

                AssetDatabase.CreateAsset(asset, assetPathAndName);
            }
            return asset;
#else
            return LoadAssetCache<TAssetCache>();
#endif
        }

        public virtual void ClearAssetCache()
        {
        }

        public virtual void UpdateAssetCache()
        {
        }

        public virtual void LoadAllAssets()
        {
        }

        protected static void CleanUpUnused<T, TValue>(Dictionary<T, TValue> dictionary, HashSet<T> unused)
        {
            foreach (T unusedKey in unused)
            {
                dictionary.Remove(unusedKey);
            }
        }

        protected static IEnumerable<T> EnumerateAllAssetsInAssetDatabase<T>(Func<string, bool> includedFileExtensions)
            where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            foreach (string assetPath in AssetDatabase.GetAllAssetPaths().Where(path => includedFileExtensions(Path.GetExtension(path).ToLowerInvariant())))
            {
                IEnumerable<T> assets = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<T>();
                if (assets != null)
                {
                    foreach (T asset in assets)
                    {
                        yield return asset;
                    }
                }
            }
#else
            yield break;
#endif
        }

        private static bool IsPrefabOrFBX(string extension)
        {
            switch (extension)
            {
                case ".prefab":
                case ".fbx":
                    return true;
                default:
                    return false;
            }
        }

        public static IEnumerable<T> EnumerateAllComponentsInScenesAndPrefabs<T>()
        {
#if UNITY_EDITOR
            foreach (string prefabPath in UnityEditor.AssetDatabase.GetAllAssetPaths().Where(path => IsPrefabOrFBX(Path.GetExtension(path).ToLowerInvariant())))
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    foreach (T descendant in prefab.GetComponentsInChildren<T>(includeInactive: true))
                    {
                        yield return descendant;
                    }
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            bool foundActiveScene = false;
            List<Scene> scenesToClose = new List<Scene>();
            for (int i = 0; i < UnityEditor.EditorBuildSettings.scenes.Length; i++)
            {
                if (UnityEditor.EditorBuildSettings.scenes[i].enabled)
                {
                    Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByBuildIndex(i);
                    foundActiveScene = foundActiveScene || scene == activeScene;
                    if (!scene.isLoaded)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(UnityEditor.EditorBuildSettings.scenes[i].path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                        scene = SceneManager.GetSceneByBuildIndex(i);
                        scenesToClose.Add(scene);
                    }

                    if (scene.IsValid())
                    {
                        var rootGameObjects = scene.GetRootGameObjects();
                        foreach (T descendant in rootGameObjects.SelectMany(go => go.GetComponentsInChildren<T>(includeInactive: true)))
                        {
                            yield return descendant;
                        }
                    }
                }
            }

            if (!foundActiveScene &&
                activeScene.IsValid())
            {
                var rootGameObjects = activeScene.GetRootGameObjects();
                foreach (T descendant in rootGameObjects.SelectMany(go => go.GetComponentsInChildren<T>(includeInactive: true)))
                {
                    yield return descendant;
                }
            }

            foreach (Scene scene in scenesToClose)
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
#else
            yield break;
#endif
        }
    }

    internal abstract class AssetCache<TAsset> : AssetCache, IAssetCache
        where TAsset : UnityEngine.Object
    {
        const string assetContentExtension = ".asset";

#pragma warning disable 414
        [SerializeField]
        private NameEntry[] assets = null;
#pragma warning restore 414

        private Dictionary<string, List<AssetId>> LookupByName
        {
            get
            {
                if (lookupByName == null)
                {
                    lookupByName = new Dictionary<string, List<AssetId>>();
                    if (assets != null)
                    {
                        for (int i = 0; i < assets.Length; i++)
                        {
                            lookupByName[assets[i].Name] = assets[i].Ids.ToList();
                        }
                    }
                }

                return lookupByName;
            }
        }

#pragma warning disable 414
        private Dictionary<string, List<AssetId>> lookupByName;
        private Dictionary<AssetId, AssetCacheEntry> lookupByAssetId = new Dictionary<AssetId, AssetCacheEntry>();
        private Dictionary<TAsset, AssetCacheEntry> lookupByAsset = new Dictionary<TAsset, AssetCacheEntry>();
#pragma warning restore 414

        public TAsset GetAsset(AssetId assetId)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.IncrementEventDuration(this.GetType().Name, "GetAsset"))
            {
                if (assetId == null ||
                assetId == AssetId.Empty ||
                assetId.Name == string.Empty)
                {
                    return default(TAsset);
                }

                if (lookupByAssetId.TryGetValue(assetId, out var entry))
                {
                    return (TAsset)entry.Asset;
                }

                if (!TryLoadAssets(assetId.Name))
                {
                    return default(TAsset);
                }

                if (!lookupByAssetId.TryGetValue(assetId, out entry))
                {
                    Debug.LogError($"Assets were loaded for {assetId.Name} but no associated asset cache entry was found for {assetId}");
                    return default(TAsset);
                }

                return (TAsset)entry.Asset;
            }
        }

        public AssetId GetAssetId(TAsset asset)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.IncrementEventDuration(this.GetType().Name, "GetAssetId"))
            {
                if (asset == null ||
                    asset.name == null ||
                    asset.name == string.Empty)
                {
                    return AssetId.Empty;
                }

                if (lookupByAsset.TryGetValue(asset, out var entry))
                {
                    return entry.AssetId;
                }

                if (!TryLoadAssets(asset.name))
                {
                    return AssetId.Empty;
                }

                if (!lookupByAsset.TryGetValue(asset, out entry))
                {
                    Debug.LogError($"Assets were loaded for {asset.name} but not associated asset cache entry was found for {asset}");
                    return AssetId.Empty;
                }

                return entry.AssetId;
            }
        }

        public override void LoadAllAssets()
        {
            using (StateSynchronizationPerformanceMonitor.Instance.IncrementEventDuration(this.GetType().Name, "LoadAllAssets"))
            {
                bool successful = true;
                foreach (var pair in LookupByName)
                {
                    if (!TryLoadAssets(pair.Key))
                    {
                        successful = false;
                    }
                }

                if (!successful)
                {
                    Debug.LogWarning($"Not all assets were found for {this.GetType().Name} during forced asset loading");
                }
            }
        }

        private bool TryLoadAssets(string name)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventMemoryUsage(this.GetType().Name, "LoadingAssets"))
            {
                if (!LookupByName.TryGetValue(name, out var assetsIds))
                {
                    return false;
                }

                string assetPath = $"{assetsFolderName}/{GetValidAssetName(name)}_{this.GetType().Name}";
                var assetCacheContent = Resources.Load<AssetCacheContent>(assetPath);
                if (assetCacheContent == null ||
                    assetCacheContent.AssetCacheEntries == null ||
                    assetCacheContent.AssetCacheEntries.Length == 0)
                {
                    Debug.LogError($"AssetCacheContent not found or empty for {name} {assetCacheContent}");
                    return false;
                }

                foreach (var entry in assetCacheContent.AssetCacheEntries)
                {
                    if (entry == null)
                    {
                        Debug.LogError($"Content in asset cache entries was null: {assetPath}");
                        continue;
                    }
                    else
                    {
                        lookupByAsset[(TAsset)entry.Asset] = entry;
                        lookupByAssetId[entry.AssetId] = entry;
                    }
                }

                return true;
            }
        }

        protected abstract IEnumerable<TAsset> EnumerateAllAssets();

        public override void ClearAssetCache()
        {
#if UNITY_EDITOR
            if (assets != null)
            {
                foreach (var nameEntry in assets)
                {
                    string assetName = GetAssetCachesContentPath($"{GetValidAssetName(nameEntry.Name)}_{this.GetType().Name}", assetContentExtension);
                    AssetDatabase.DeleteAsset(assetName);
                }
            }

            assets = null;
            lookupByName = null;
            lookupByAsset = null;
            lookupByAssetId = null;

            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Assets are ordered in their associated AssetCache by AssetId (first by file identifier, then by guid).
        /// </summary>
        public override void UpdateAssetCache()
        {
#if UNITY_EDITOR
            Dictionary<string, List<AssetId>> oldAssets = lookupByName == null ? new Dictionary<string, List<AssetId>>() : lookupByName;
            HashSet<Tuple<string, AssetId>> unvisitedIds = new HashSet<Tuple<string, AssetId>>();
            lookupByAsset = new Dictionary<TAsset, AssetCacheEntry>();
            lookupByAssetId = new Dictionary<AssetId, AssetCacheEntry>();
            foreach (var listPair in oldAssets)
            {
                foreach (var id in listPair.Value)
                {
                    unvisitedIds.Add(new Tuple<string, AssetId>(listPair.Key, id));
                }
            }

            foreach (TAsset asset in EnumerateAllAssets())
            {
                if (asset.name == null ||
                    asset.name == string.Empty ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localid))
                {
                    Debug.LogError($"Unable to identify asset: {asset}");
                    continue;
                }

                var assetId = new AssetId(new Guid(guid), localid, asset.name);
                unvisitedIds.Remove(new Tuple<string, AssetId>(assetId.Name, assetId));

                if (!oldAssets.ContainsKey(assetId.Name))
                {
                    oldAssets[assetId.Name] = new List<AssetId>();
                    oldAssets[assetId.Name].Add(assetId);
                }
                else if (!oldAssets[assetId.Name].Contains(assetId))
                {
                    oldAssets[assetId.Name].Add(assetId);
                }

                var entry = new AssetCacheEntry();
                entry.Asset = asset;
                entry.AssetId = assetId;
                lookupByAsset[asset] = entry;
                lookupByAssetId[assetId] = entry;
            }

            foreach (var idPair in unvisitedIds)
            {
                if (oldAssets.TryGetValue(idPair.Item1, out List<AssetId> ids) &&
                    ids.Contains(idPair.Item2))
                {
                    ids.Remove(idPair.Item2);
                    if (ids.Count == 0)
                    {
                        string assetName = GetAssetCachesContentPath($"{GetValidAssetName(idPair.Item1)}_{this.GetType().Name}", assetContentExtension);
                        AssetDatabase.DeleteAsset(assetName);
                    }
                }
            }

            lookupByName = oldAssets;
            var tempAssets = new List<NameEntry>();
            foreach (var item in lookupByName)
            {
                tempAssets.Add(new NameEntry(item.Key, item.Value.OrderBy(a => a.Guid).ThenBy(a => a.FileIdentifier).ToArray()));
            }
            assets = tempAssets.OrderBy(a => a.Name).ToArray();
            EditorUtility.SetDirty(this);
#endif
        }

        public virtual void SaveAssets()
        {
#if UNITY_EDITOR
            EnsureAssetDirectoryExists();
            if (assets == null ||
                assets.Length == 0)
            {
                Debug.Log("Assets was null or empty, rerunning asset cache update");
                UpdateAssetCache();
            }

            if (assets == null ||
                assets.Length == 0)
            {
                Debug.Log("No assets were found.");
                return;
            }

            foreach (var nameEntry in assets)
            {
                string assetName = GetAssetCachesContentPath($"{GetValidAssetName(nameEntry.Name)}_{this.GetType().Name}", assetContentExtension);
                AssetCacheContent content = ScriptableObject.CreateInstance<AssetCacheContent>();
                List<AssetCacheEntry> assetCacheEntries = new List<AssetCacheEntry>();
                foreach (var assetId in nameEntry.Ids)
                {
                    if (!lookupByAssetId.TryGetValue(assetId, out var entry))
                    {
                        Debug.LogError($"AssetId did not have a registered Asset: {assetId}");
                        continue;
                    }

                    assetCacheEntries.Add(entry);
                }

                content.AssetCacheEntries = assetCacheEntries.ToArray();
                Debug.Log($"Creating asset: {content} {assetName}");
                AssetDatabase.CreateAsset(content, assetName);
            }
#endif
        }

        string GetValidAssetName(string name)
        {
            if (name == null)
            {
                return null;
            }

            var invalidChars = Path.GetInvalidPathChars();
            string updatedName = name;
            foreach(var invalid in invalidChars)
            {
                updatedName = updatedName.Replace(invalid.ToString(), "");
            }

            return updatedName;
        }
    }
}
