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
        [Tooltip("Prefab for creating a mobile recording service visual.")]
        [SerializeField]
        private GameObject mobileRecordingServiceVisualPrefab = null;

        public bool EnableMobileRecordingService => enableMobileRecordingService;

        public GameObject MobileRecordingServicePrefab => mobileRecordingServiceVisualPrefab;
    }
}