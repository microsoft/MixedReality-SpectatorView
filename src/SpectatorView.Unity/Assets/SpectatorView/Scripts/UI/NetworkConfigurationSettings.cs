// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class NetworkConfigurationSettings : Singleton<NetworkConfigurationSettings>
    {
        [Tooltip("Check to enable a mobile network configuration visual to obtain the user IP Address.")]
        [SerializeField]
        private bool enableMobileNetworkConfigurationVisual = true;

        [Tooltip("Prefab for creating amobile network configuration visual, which replaces the defaultMobileNetworkConfigurationVisualPrefab on the SpectatorView component if set.")]
        [SerializeField]
        private GameObject overrideMobileNetworkConfigurationVisualPrefab = null;

        /// <summary>
        /// When true, a mobile network configuration visual is used to obtain the user IP Address.
        /// </summary>
        public bool EnableMobileNetworkConfigurationVisual => enableMobileNetworkConfigurationVisual;

        /// <summary>
        /// Prefab for creating a mobile network configuration visual.
        /// </summary>
        public GameObject OverrideMobileNetworkConfigurationVisualPrefab => overrideMobileNetworkConfigurationVisualPrefab;
    }
}
