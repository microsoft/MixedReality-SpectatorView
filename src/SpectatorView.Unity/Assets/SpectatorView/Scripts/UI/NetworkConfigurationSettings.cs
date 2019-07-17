// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class NetworkConfigurationSettings : Singleton<NetworkConfigurationSettings>
    {
        /// <summary>
        /// Check to enable a mobile network configuration visual to obtain the user IP Address.
        /// </summary>
        [Tooltip("Check to enable a mobile network configuration visual to obtain the user IP Address.")]
        [SerializeField]
        private bool enableMobileNetworkConfigurationVisual = true;

        /// <summary>
        /// Prefab for creating a mobile network configuration visual.
        /// </summary>
        [Tooltip("Prefab for creating amobile network configuration visual, which replaces the defaultMobileNetworkConfigurationVisualPrefab on the SpectatorView component if set.")]
        [SerializeField]
        private GameObject overrideMobileNetworkConfigurationVisualPrefab = null;

        public bool EnableMobileNetworkConfigurationVisual => enableMobileNetworkConfigurationVisual;

        public GameObject OverrideMobileNetworkConfigurationVisualPrefab => overrideMobileNetworkConfigurationVisualPrefab;
    }
}
