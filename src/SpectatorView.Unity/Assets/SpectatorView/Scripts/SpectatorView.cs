// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Microsoft.MixedReality.SpectatorView.Editor")]

namespace Microsoft.MixedReality.SpectatorView
{
    public enum Role
    {
        User,
        Spectator
    }

    /// <summary>
    /// Class that facilitates the Spectator View experience
    /// </summary>
    public class SpectatorView : MonoBehaviour
    {
        public const string SettingsPrefabName = "SpectatorViewSettings";

        /// <summary>
        /// Role of the device in the spectator view experience.
        /// </summary>
        [Tooltip("Role of the device in the spectator view experience.")]
        [SerializeField]
        public Role Role;

        [Header("Networking")]
        /// <summary>
        /// User ip address
        /// </summary>
        [Tooltip("User ip address")]
        [SerializeField]
        private string userIpAddress = "127.0.0.1";

        [Header("State Synchronization")]
        /// <summary>
        /// StateSynchronizationSceneManager MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationSceneManager")]
        [SerializeField]
        private StateSynchronizationSceneManager stateSynchronizationSceneManager = null;

        /// <summary>
        /// StateSynchronizationBroadcaster MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationBroadcaster MonoBehaviour")]
        [SerializeField]
        private StateSynchronizationBroadcaster stateSynchronizationBroadcaster = null;

        /// <summary>
        /// StateSynchronizationObserver MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationObserver MonoBehaviour")]
        [SerializeField]
        private StateSynchronizationObserver stateSynchronizationObserver = null;

        [Header("Recording")]
        /// <summary>
        /// Prefab for creating a mobile recording service visual.
        /// </summary>
        [Tooltip("Default prefab for creating a mobile recording service visual.")]
        [SerializeField]
        public GameObject defaultMobileRecordingServiceVisualPrefab = null;

        [Header("Debugging")]
        /// <summary>
        /// Debug visual prefab created by the user.
        /// </summary>
        [Tooltip("Debug visual prefab created by the user.")]
        [SerializeField]
        public GameObject userDebugVisualPrefab = null;

        /// <summary>
        /// Scaling applied to user debug visuals.
        /// </summary>
        [Tooltip("Scaling applied to spectator debug visuals.")]
        [SerializeField]
        public float userDebugVisualScale = 1.0f;

        /// <summary>
        /// Debug visual prefab created by the spectator.
        /// </summary>
        [Tooltip("Debug visual prefab created by the spectator.")]
        [SerializeField]
        public GameObject spectatorDebugVisualPrefab = null;

        /// <summary>
        /// Scaling applied to spectator debug visuals.
        /// </summary>
        [Tooltip("Scaling applied to spectator debug visuals.")]
        [SerializeField]
        public float spectatorDebugVisualScale = 1.0f;

#if UNITY_ANDROID || UNITY_IOS
        private GameObject mobileRecordingServiceVisual = null;
        private IRecordingService recordingService = null;
        private IRecordingServiceVisual recordingServiceVisual = null;
#endif

        private void Awake()
        {
            Debug.Log($"SpectatorView is running as: {Role.ToString()}. Expected User IPAddress: {userIpAddress}");

            GameObject settings = Resources.Load<GameObject>(SettingsPrefabName);
            if (settings != null)
            {
                Instantiate(settings, null);
            }

            if (stateSynchronizationSceneManager == null ||
                stateSynchronizationBroadcaster == null ||
                stateSynchronizationObserver == null)
            {
                Debug.LogError("StateSynchronization scene isn't configured correctly");
                return;
            }

            switch (Role)
            {
                case Role.User:
                    {
                        if (userDebugVisualPrefab != null)
                        {
                            SpatialCoordinateSystemManager.Instance.debugVisual = userDebugVisualPrefab;
                            SpatialCoordinateSystemManager.Instance.debugVisualScale = userDebugVisualScale;
                        }

                        RunStateSynchronizationAsBroadcaster();
                    }
                    break;
                case Role.Spectator:
                    {
                        if (spectatorDebugVisualPrefab != null)
                        {
                            SpatialCoordinateSystemManager.Instance.debugVisual = spectatorDebugVisualPrefab;
                            SpatialCoordinateSystemManager.Instance.debugVisualScale = spectatorDebugVisualScale;
                        }

                        // When running as a spectator, automatic localization should be initiated if it's configured.
                        if (SpatialLocalizationInitializationSettings.IsInitialized)
                        {
                            SpatialLocalizationInitializationSettings.Instance.ConfigureAutomaticLocalization();
                        }

                        RunStateSynchronizationAsObserver();
                    }
                    break;
            }

            SetupRecordingService();
        }

        private void RunStateSynchronizationAsBroadcaster()
        {
            stateSynchronizationBroadcaster.gameObject.SetActive(true);
            stateSynchronizationObserver.gameObject.SetActive(false);

            // The StateSynchronizationSceneManager needs to be enabled after the broadcaster/observer
            stateSynchronizationSceneManager.gameObject.SetActive(true);
        }

        private void RunStateSynchronizationAsObserver()
        {
            stateSynchronizationBroadcaster.gameObject.SetActive(false);
            stateSynchronizationObserver.gameObject.SetActive(true);

            // The StateSynchronizationSceneManager needs to be enabled after the broadcaster/observer
            stateSynchronizationSceneManager.gameObject.SetActive(true);

            // Make sure the StateSynchronizationSceneManager is enabled prior to connecting the observer
            stateSynchronizationObserver.ConnectTo(userIpAddress);
        }

        private void SetupRecordingService()
        {
#if UNITY_ANDROID || UNITY_IOS
            GameObject recordingVisualPrefab = defaultMobileRecordingServiceVisualPrefab;
            if (MobileRecordingSettings.IsInitialized && MobileRecordingSettings.Instance.OverrideMobileRecordingServicePrefab != null)
            {
                recordingVisualPrefab = MobileRecordingSettings.Instance.OverrideMobileRecordingServicePrefab;
            }

            if (MobileRecordingSettings.IsInitialized && 
                MobileRecordingSettings.Instance.EnableMobileRecordingService &&
                recordingVisualPrefab != null)
            {
                mobileRecordingServiceVisual = Instantiate(recordingVisualPrefab);

                if (!TryCreateRecordingService(out recordingService))
                {
                    Debug.LogError("Failed to create a recording service for the current platform.");
                    return;
                }

                recordingServiceVisual = mobileRecordingServiceVisual.GetComponentInChildren<IRecordingServiceVisual>();
                if (recordingServiceVisual == null)
                {
                    Debug.LogError("Failed to find an IRecordingServiceVisual in the created mobileRecordingServiceVisualPrefab. Note: It's assumed that the IRecordingServiceVisual is enabled by default in the mobileRecordingServiceVisualPrefab.");
                    return;
                }

                recordingServiceVisual.SetRecordingService(recordingService);
            }
#endif
        }

        private bool TryCreateRecordingService(out IRecordingService recordingService)
        {
#if UNITY_ANDROID
            recordingService = new AndroidRecordingService();
            return true;
#elif UNITY_IOS
            recordingService = new iOSRecordingService();
            return true;
#else
            recordingService = null;
            return false;
#endif
        }
    }
}
