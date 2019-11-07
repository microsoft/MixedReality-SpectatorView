// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    internal abstract class AssetCache : ScriptableObject
    {
        const string assetCacheDirectory = "Generated.StateSynchronization.AssetCaches";

        public static TAssetCache LoadAssetCache<TAssetCache>()
            where TAssetCache : AssetCache
        {
            return Resources.Load<TAssetCache>(typeof(TAssetCache).Name);
        }

        public static string GetAssetPath(string assetName, string assetExtension)
        {
            return $"Assets/{assetCacheDirectory}/Resources/" + assetName + assetExtension;
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
#endif
        }

        public static TAssetCache GetOrCreateAssetCache<TAssetCache>()
            where TAssetCache : AssetCache
        {
#if UNITY_EDITOR
            string assetPathAndName = GetAssetPath(typeof(TAssetCache).Name, ".asset");

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

    internal abstract class AssetCache<TAssetEntry, TAsset> : AssetCache, IAssetCache
        where TAssetEntry : AssetCacheEntry<TAsset>, new()
        where TAsset : UnityEngine.Object
    {
        [SerializeField]
        private TAssetEntry[] assets = null;

        private Dictionary<AssetId, TAssetEntry> lookupByAssetId;
        private Dictionary<TAsset, TAssetEntry> lookupByAsset;

        protected IDictionary<AssetId, TAssetEntry> LookupByAssetId
        {
            get
            {
                if (lookupByAssetId == null)
                {
                    if (assets == null)
                    {
                        lookupByAssetId = new Dictionary<AssetId, TAssetEntry>();
                    }
                    else
                    {
                        lookupByAssetId = assets.Where(a => a.Asset != null).ToDictionary(a => a.AssetId);
                    }
                }
                return lookupByAssetId;
            }
        }

        protected IDictionary<TAsset, TAssetEntry> LookupByAsset
        {
            get
            {
                if (lookupByAsset == null)
                {
                    if (assets == null)
                    {
                        lookupByAsset = new Dictionary<TAsset, TAssetEntry>();
                    }
                    else
                    {
                        lookupByAsset = assets.Where(a => a.Asset != null).ToDictionary(a => a.Asset);
                    }
                }
                return lookupByAsset;
            }
        }

        public TAsset GetAsset(AssetId assetId)
        {
            TAssetEntry assetEntry;
            if (LookupByAssetId.TryGetValue(assetId, out assetEntry))
            {
                return assetEntry.Asset;
            }
            else
            {
                return default(TAsset);
            }
        }

        public AssetId GetAssetId(TAsset asset)
        {
            TAssetEntry assetEntry;
            if (asset != null && LookupByAsset.TryGetValue(asset, out assetEntry))
            {
                return assetEntry.AssetId;
            }
            else
            {
                return AssetId.Empty;
            }
        }

        protected abstract IEnumerable<TAsset> EnumerateAllAssets();

        public override void ClearAssetCache()
        {
#if UNITY_EDITOR
            assets = null;
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
            Dictionary<TAsset, TAssetEntry> oldAssets = (assets ?? Array.Empty<TAssetEntry>()).Where(a => a.Asset != null).ToDictionary(a => a.Asset);

            HashSet<TAsset> unvisitedAssets = new HashSet<TAsset>(assets == null ? Enumerable.Empty<TAsset>() : assets.Where(a => a.Asset != null).Select(a => a.Asset));

            foreach (TAsset asset in EnumerateAllAssets())
            {
                unvisitedAssets.Remove(asset);
                if (!oldAssets.ContainsKey(asset))
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localid))
                    {
                        oldAssets.Add(asset, new TAssetEntry
                        {
                            AssetId = new AssetId(new Guid(guid), localid),
                            Asset = asset
                        });
                    }
                    else
                    {
                        Debug.LogError($"Unable to obtain id for Asset: {asset.ToString()}");
                    }
                }
            }

            CleanUpUnused(oldAssets, unvisitedAssets);
            assets = oldAssets.Values.OrderBy(x => x.AssetId.FileIdentifier).ThenBy(x => x.AssetId.Guid).ToArray();

            EditorUtility.SetDirty(this);
#endif
        }
    }
}
