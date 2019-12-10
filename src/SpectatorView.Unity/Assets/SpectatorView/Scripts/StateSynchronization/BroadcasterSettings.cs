// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Settings used by a <see cref="StateSynchronizationBroadcaster"/>
    /// </summary>
    public class BroadcasterSettings : Singleton<BroadcasterSettings>
    {
        /// <summary>
        /// Determines whether or not all GameObjects are synchronized or only those with a GameObjectHierarchyBroadcaster are synchronized.
        /// </summary>
        [SerializeField]
        [Tooltip("Determines whether or not all GameObjects are synchronized or only those with a GameObjectHierarchyBroadcaster are synchronized.")]
        private bool automaticallyBroadcastAllGameObjects = false;

        /// <summary>
        /// Determines whether or not all GameObjects are synchronized or only those with a GameObjectHierarchyBroadcaster are synchronized.
        /// </summary>
        public bool AutomaticallyBroadcastAllGameObjects
        {
            get { return automaticallyBroadcastAllGameObjects; }
        }


        [SerializeField]
        [Tooltip("Check to force performance reporting at compile time so that measures can be made on startup taken prior to connecting to the broadcaster device. Note: enabling this parameter will decrease the overall performance of the user application.")]
        private bool forcePerformanceReporting = false;

        /// <summary>
        /// Forces performance reporting at compile time so that on start up measures can be taken prior to connecting to the broadcaster device.
        /// </summary>
        public bool ForcePerformanceReporting
        {
            get { return forcePerformanceReporting; }
        }

        [SerializeField]
        [Tooltip("Check to force loading all assets in asset caches on initialization. Note: this will improve the performance of identifying assets for sending asset changes between devices, but it will require using more memory.")]
        private bool forceLoadAllAssetsDuringInitialization = false;

        /// <summary>
        /// Forces loading all assets in asset caches on initialization. Note: this will improve the performance of identifying assets for sending asset changes between devices, but it will require using more memory.
        /// </summary>
        public bool ForceLoadAllAssetsDuringInitialization
        {
            get { return forceLoadAllAssetsDuringInitialization; }
        }
    }
}
