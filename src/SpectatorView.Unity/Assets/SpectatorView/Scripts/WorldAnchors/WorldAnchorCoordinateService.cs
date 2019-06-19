using Microsoft.MixedReality.Experimental.SpatialAlignment.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_WSA
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
#endif

namespace Microsoft.MixedReality.Toolkit.Extensions.Experimental.SpectatorView.WorldAnchors
{
    /// <summary>
    /// Represents an <see cref="ISpatialCoordinateService"/> that creates and stores
    /// coordinates based on WorldAnchors stored in a WorldAnchorStore on the device.
    /// </summary>
    public class WorldAnchorCoordinateService : SpatialCoordinateServiceUnityBase<string>
    {
        private static Task<WorldAnchorCoordinateService> sharedCoordinateService;
#if UNITY_WSA
        private Task<WorldAnchorStore> worldAnchorStoreTask;
#endif

        /// <summary>
        /// Constructs the coordinate service. Access to the coordinate service
        /// should be done through GetSharedCoordinateServiceAsync.
        /// </summary>
        private WorldAnchorCoordinateService()
        {
        }
        
        protected override bool SupportsDiscovery => false;

        public static Task<WorldAnchorCoordinateService> GetSharedCoordinateServiceAsync()
        {
            if (sharedCoordinateService == null)
            {
                sharedCoordinateService = InitializeSharedCoordinateServiceAsync();
            }

            return sharedCoordinateService;
        }

        private static async Task<WorldAnchorCoordinateService> InitializeSharedCoordinateServiceAsync()
        {
            WorldAnchorCoordinateService service = new WorldAnchorCoordinateService();
            await service.InitializeKnownCoordinatesAsync();
            return service;
        }

        protected override Task OnDiscoverCoordinatesAsync(CancellationToken cancellationToken, string[] idsToLocate = null)
        {
            throw new NotSupportedException("WorldAnchorCoordinateService does not support discovery");
        }

        protected override bool TryParse(string id, out string result)
        {
            result = id;
            return true;
        }

        public async Task InitializeKnownCoordinatesAsync()
        {
#if UNITY_WSA
            WorldAnchorStore store = await GetWorldAnchorStoreAsync();

            foreach (string knownId in store.GetAllIds())
            {
                GameObject anchorGameObject = new GameObject(knownId);
                WorldAnchor anchor = store.Load(knownId, anchorGameObject);

                if (anchor == null)
                {
                    Debug.LogError($"Unexpected WorldAnchor {knownId} was enumerated but failed to load");
                    continue;
                }

                OnNewCoordinate(knownId, new WorldAnchorSpatialCoordinate(knownId, anchor));
            }
#else
            await Task.CompletedTask;
#endif
        }

        public async Task<ISpatialCoordinate> CreateCoordinateAsync(string id, Vector3 worldPosition, Quaternion worldRotation, CancellationToken cancellationToken)
        {
#if UNITY_WSA
            await TryDeleteCoordinateAsync(id, cancellationToken);

            GameObject gameObject = new GameObject(id);
            gameObject.transform.position = worldPosition;
            gameObject.transform.rotation = worldRotation;
            WorldAnchor anchor = gameObject.AddComponent<WorldAnchor>();

            if (anchor.isLocated)
            {
                WorldAnchorStore store = await GetWorldAnchorStoreAsync();
                store.Save(id, anchor);
            }
            else
            {
                anchor.OnTrackingChanged += Anchor_OnTrackingChanged;
            }

            WorldAnchorSpatialCoordinate coordinate = new WorldAnchorSpatialCoordinate(id, anchor);
            OnNewCoordinate(id, coordinate);
            return coordinate;
#else
            return await Task.FromResult<ISpatialCoordinate>(null);
#endif
        }

        public override async Task<bool> TryDeleteCoordinateAsync(string key, CancellationToken cancellationToken)
        {
#if UNITY_WSA
            if (TryGetKnownCoordinate(key, out ISpatialCoordinate coordinate))
            {
                WorldAnchorSpatialCoordinate worldAnchorCoordinate = (WorldAnchorSpatialCoordinate)coordinate;
                WorldAnchorStore anchorStore = await GetWorldAnchorStoreAsync();
                anchorStore.Delete(key);
                worldAnchorCoordinate.Destroy();
            }
#endif

            return await base.TryDeleteCoordinateAsync(key, cancellationToken);
        }

#if UNITY_WSA
        private async void Anchor_OnTrackingChanged(WorldAnchor worldAnchor, bool located)
        {
            if (located)
            {
                worldAnchor.OnTrackingChanged -= Anchor_OnTrackingChanged;
                WorldAnchorStore store = await GetWorldAnchorStoreAsync();
                store.Save(worldAnchor.name, worldAnchor);
            }
        }

        protected override void OnManagedDispose()
        {
            base.OnManagedDispose();

            foreach (WorldAnchorSpatialCoordinate coordinate in KnownCoordinates)
            {
                coordinate.Destroy();
            }

            if (worldAnchorStoreTask.IsCompleted)
            {
                worldAnchorStoreTask.Result.Dispose();
                worldAnchorStoreTask = null;
            }
        }

        private Task<WorldAnchorStore> GetWorldAnchorStoreAsync()
        {
            if (worldAnchorStoreTask == null)
            {
                worldAnchorStoreTask = InitializeWorldAnchorStoreAsync();
            }

            return worldAnchorStoreTask;
        }

        private Task<WorldAnchorStore> InitializeWorldAnchorStoreAsync()
        {
            TaskCompletionSource<WorldAnchorStore> taskSource = new TaskCompletionSource<WorldAnchorStore>();
            WorldAnchorStore.GetAsync(store => taskSource.SetResult(store));
            return taskSource.Task;
        }

        private class WorldAnchorSpatialCoordinate : SpatialCoordinateUnityBase<string>
        {
            private WorldAnchor worldAnchor;

            public WorldAnchorSpatialCoordinate(string id, WorldAnchor worldAnchor)
                : base(id)
            {
                this.worldAnchor = worldAnchor;
                this.worldAnchor.OnTrackingChanged += WorldAnchor_OnTrackingChanged;
            }

            public override LocatedState State
            {
                get
                {
                    if (worldAnchor == null)
                    {
                        // Once the WorldAnchor has been destroyed,
                        // the coordinate will live in a Resolved state
                        // at the last-known position of the WorldAnchor.
                        return LocatedState.Resolved;
                    }
                    else if (!worldAnchor.isLocated)
                    {
                        // When the WorldAnchor exists but is no longer located,
                        // report the state of the coordinate as Inhibited.
                        return LocatedState.Inhibited;
                    }
                    else
                    {
                        // The WorldAnchor exists and is currently located,
                        // so report the state of the coordinate as Tracking.
                        return LocatedState.Tracking;
                    }
                }
            }

            private void WorldAnchor_OnTrackingChanged(WorldAnchor worldAnchor, bool located)
            {
                OnStateChanged();
            }

            protected override Quaternion CoordinateToWorldSpace(Quaternion quaternion)
            {
                if (worldAnchor != null)
                {
                    return worldAnchor.transform.rotation * quaternion;
                }

                return base.CoordinateToWorldSpace(quaternion);
            }

            protected override Vector3 CoordinateToWorldSpace(Vector3 vector)
            {
                if (worldAnchor != null)
                {
                    return worldAnchor.transform.TransformPoint(vector);
                }

                return base.CoordinateToWorldSpace(vector);
            }

            protected override Quaternion WorldToCoordinateSpace(Quaternion quaternion)
            {
                if (worldAnchor != null)
                {
                    return Quaternion.Inverse(worldAnchor.transform.rotation) * quaternion;
                }

                return base.WorldToCoordinateSpace(quaternion);
            }

            protected override Vector3 WorldToCoordinateSpace(Vector3 vector)
            {
                if (worldAnchor != null)
                {
                    return worldAnchor.transform.InverseTransformPoint(vector);
                }

                return base.WorldToCoordinateSpace(vector);
            }

            public void Destroy()
            {
                worldAnchor.OnTrackingChanged -= WorldAnchor_OnTrackingChanged;
                SetCoordinateWorldTransform(worldAnchor.transform.position, worldAnchor.transform.rotation);

                GameObject.Destroy(worldAnchor.gameObject);
                worldAnchor = null;
            }
        }
#endif
    }
}