// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MobileRecordingSettings : Singleton<MobileRecordingSettings>
    {
        /// <summary>
        /// Check to enable the mobile recording service.
        /// </summary>
        [Tooltip("Check to enable the mobile recording service.")]
        [SerializeField]
        private bool enableMobileRecordingService = true;

        /// <summary>
        /// Prefab for creating a mobile recording service visual.
        /// </summary>
        [Tooltip("Prefab for creating a mobile recording service visual, which replaces the defaultMobileRecordingServiceVisualPrefab on the SpectatorView component if set.")]
        [SerializeField]
        private GameObject overrideMobileRecordingServiceVisualPrefab = null;

        public bool EnableMobileRecordingService => enableMobileRecordingService;

        public GameObject OverrideMobileRecordingServicePrefab => overrideMobileRecordingServiceVisualPrefab;
    }
}