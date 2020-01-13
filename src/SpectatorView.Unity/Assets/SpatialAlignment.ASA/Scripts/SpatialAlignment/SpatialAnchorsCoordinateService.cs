// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpatialAlignment
{
    /// <summary>
    /// Spatial Coordinate Service for Azure Spatial Anchors support.
    /// </summary>
    public class SpatialAnchorsCoordinateService : SpatialCoordinateServiceUnityBase<string>
    {
        private readonly SynchronizationContext gameThreadSynchronizationContext;
        private GameObject parent;
        private SpatialAnchorsConfiguration spatialAnchorsConfiguration;
        private SpatialAnchorManager spatialAnchorManager;
        private object sessionStartLock = new object();
        private Task sessionStartTask;

        public SpatialAnchorsCoordinateService(GameObject parent, SpatialAnchorsConfiguration spatialAnchorsConfiguration)
        {
            this.gameThreadSynchronizationContext = SynchronizationContext.Current;
            this.parent = parent;
            this.spatialAnchorsConfiguration = spatialAnchorsConfiguration;
            if (this.parent != null)
            {
                Debug.Log("SpatialAnchorsCoordinateService: Creating a CustomSpatialAnchorManager.");
                SpatialAnchorConfig config = CustomSpatialAnchorConfig.Create(spatialAnchorsConfiguration.AccountId, spatialAnchorsConfiguration.AccountKey);
                spatialAnchorManager = this.parent.AddCustomSpatialAnchorManager(config);
                StartSession().FireAndForget();
            }
        }

        /// <inheritdoc/>
        protected override void OnManagedDispose()
        {
            base.OnManagedDispose();

            if (spatialAnchorManager != null)
            {
                Debug.Log("SpatialAnchorsCoordinateService: Cleaning up SpatialAnchorManager.");
                StopSession().FireAndForget();
                GameObject.Destroy(spatialAnchorManager); // Destroying the SpatialAnchorManager should destroy the session.
                spatialAnchorManager = null;
            }
        }

        /// <inheritdoc/>
        protected override async Task<ISpatialCoordinate> TryCreateCoordinateAsync(Vector3 worldPosition, Quaternion worldRotation, CancellationToken cancellationToken)
        {
            GameObject spawnedAnchorObject = SpawnGameObject(worldPosition, worldRotation);
            try
            {
                // Use var here, type varies based on platform
                var nativeAnchor = spawnedAnchorObject.FindOrCreateNativeAnchor();

                // Let a frame pass to ensure any AR anchor is properly attached (WorldAnchors used to have issues with this)
                await Task.Delay(100, cancellationToken);

                CloudSpatialAnchor cloudSpatialAnchor = new CloudSpatialAnchor()
                {
                    LocalAnchor = nativeAnchor.GetPointer(),
                    Expiration = DateTime.Now.AddDays(1)
                };

                if (cloudSpatialAnchor.LocalAnchor == IntPtr.Zero)
                {
                    Debug.LogError($"{nameof(SpatialAnchorsCoordinateService)} failed to get native anchor pointer when creating anchor.");
                    return null;
                }

                await spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor, cancellationToken);
                return new SpatialAnchorsCoordinate(cloudSpatialAnchor, nativeAnchor.gameObject);
            }
            catch
            {
                UnityEngine.Object.Destroy(spawnedAnchorObject);
                throw;
            }
        }

        /// <inheritdoc/>
        protected override async Task OnDiscoverCoordinatesAsync(CancellationToken cancellationToken, string[] idsToLocate = null)
        {
            try
            {
                AnchorLocateCriteria anchorLocateCriteria = new AnchorLocateCriteria();
                HashSet<string> ids = new HashSet<string>();
                if (idsToLocate?.Length > 0)
                {
                    anchorLocateCriteria.Identifiers = idsToLocate;
                    for (int i = 0; i < idsToLocate.Length; i++)
                    {
                        if (!knownCoordinates.ContainsKey(idsToLocate[i]))
                        {
                            ids.Add(idsToLocate[i]);
                        }
                    }

                    if (ids.Count == 0)
                    {
                        // We know already all of the coordinates
                        return;
                    }
                }

                using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    // Local handler
                    async void AnchorLocatedHandler(object sender, AnchorLocatedEventArgs args)
                    {
                        if (args.Status == LocateAnchorStatus.Located)
                        {
                            // Switch to UI thread for the rest here
                            await gameThreadSynchronizationContext;

                            GameObject gameObject = CreateGameObjectFrom(args);

                            SpatialAnchorsCoordinate coordinate = new SpatialAnchorsCoordinate(args.Anchor, gameObject);
                            OnNewCoordinate(coordinate.Id, coordinate);

                            lock (ids)
                            {
                                // If we succefully removed one and we are at 0, then stop. 
                                // If we never had to locate any, we would always be at 0 but never remove any.
                                if (ids.Remove(args.Identifier) && ids.Count == 0)
                                {
                                    // We found all requested, stop
                                    cts.Cancel();
                                }
                            }
                        }
                    }

                    spatialAnchorManager.Session.AnchorLocated += AnchorLocatedHandler;
                    CloudSpatialAnchorWatcher watcher = spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
                    try
                    {
                        await cts.Token.AsTask().IgnoreCancellation();
                    }
                    finally
                    {
                        spatialAnchorManager.Session.AnchorLocated -= AnchorLocatedHandler;
                        watcher.Stop();
                    }
                }
            }
            finally
            {
            }
        }

        /// <inheritdoc/>
        public override Task<bool> TryDeleteCoordinateAsync(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override bool TryParse(string id, out string result)
        {
            result = id;
            return true;
        }

        /// <summary>
        /// Simple helper method to spawn anchor <see cref="GameObject"/> provided position and rotation.
        /// </summary>
        /// <param name="worldPosition">Position of the anchor.</param>
        /// <param name="worldRotation">Rotation of the anchor.</param>
        /// <returns>The newly spawned <see cref="GameObject"/>.</returns>
        protected GameObject SpawnGameObject(Vector3 worldPosition, Quaternion worldRotation)
        {
            GameObject spawnedAnchorObject = new GameObject("Azure Spatial Anchor");
            UnityEngine.Object.DontDestroyOnLoad(spawnedAnchorObject);

            spawnedAnchorObject.transform.position = worldPosition;
            spawnedAnchorObject.transform.rotation = worldRotation;

            return spawnedAnchorObject;
        }

        /// <summary>
        /// Creates a <see cref="GameObject"/> representing the anchor based on provided <see cref="AnchorLocatedEventArgs"/>.
        /// </summary>
        /// <param name="args">Args passed from AnchorLocated event.</param>
        /// <returns>The newly created <see cref="GameObject"/>.</returns>
        protected virtual GameObject CreateGameObjectFrom(AnchorLocatedEventArgs args)
        {
            Pose pose = args.Anchor.GetPose();
            Debug.Log($"ASA-Android: Creating an anchor at: {pose.position.ToString("G4")}, {pose.rotation.eulerAngles.ToString("G2")}");
            GameObject gameObject = SpawnGameObject(pose.position, pose.rotation);
            gameObject.FindOrCreateNativeAnchor();

            return gameObject;
        }

        private Task StartSession()
        {
            lock (sessionStartLock)
            {
                if (sessionStartTask == null)
                {
                    sessionStartTask = spatialAnchorManager.StartSessionAsync();
                }
            }

            return sessionStartTask;
        }

        private async Task StopSession()
        {
            Task tempStartTask = null;
            lock (sessionStartLock)
            {
                if (sessionStartTask != null)
                {
                    tempStartTask = sessionStartTask;
                }
            }

            if (tempStartTask != null)
            {
                await Task.WhenAll(tempStartTask);
            }

            spatialAnchorManager.StopSession();
        }
    }
}