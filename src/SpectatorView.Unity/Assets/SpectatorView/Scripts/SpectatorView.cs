// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

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
        /// User ip address, this value is used for the user if 'Use Mobile Network Configuration Visual' is set to false.
        /// </summary>
        [Tooltip("User ip address, this value is used for the user if 'Use Mobile Network Configuration Visual' is set to false.")]
        [SerializeField]
        private string userIpAddress = "127.0.0.1";

        /// <summary>
        /// Prefab for creating a mobile network configuration visual.
        /// </summary>
        [Tooltip("Default prefab for creating a mobile network configuration visual.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject defaultMobileNetworkConfigurationVisualPrefab = null;
#pragma warning restore 414

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

        [Header("Spatial Alignment")]
        [Tooltip("A prioritized list of SpatialLocalizationInitializers that should be used when a spectator connects.")]
        [SerializeField]
        private SpatialLocalizationInitializer[] defaultSpatialLocalizationInitializers = null;

        [Header("Recording")]
        /// <summary>
        /// Prefab for creating a mobile recording service visual.
        /// </summary>
        [Tooltip("Default prefab for creating a mobile recording service visual.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject defaultMobileRecordingServiceVisualPrefab = null;
#pragma warning restore 414

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

        [Tooltip("Enable verbose debug logging messages")]
        [SerializeField]
        private bool debugLogging = false;

        [Tooltip("Check to hide the development console every update.")]
        [SerializeField]
        private bool hideDevelopmentConsole = false;

        private GameObject settingsGameObject;

#if UNITY_ANDROID || UNITY_IOS
        private GameObject mobileRecordingServiceVisual = null;
        private IRecordingService recordingService = null;
        private IRecordingServiceVisual recordingServiceVisual = null;
        private GameObject mobileNetworkConfigurationVisual = null;
        private INetworkConfigurationVisual networkConfigurationVisual = null;
#endif

        private void Awake()
        {
            Debug.Log($"SpectatorView is running as: {Role.ToString()}. Expected User IPAddress: {userIpAddress}");

            GameObject settings = Resources.Load<GameObject>(SettingsPrefabName);
            if (settings != null)
            {
                settingsGameObject = Instantiate(settings, null);
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
                        SpatialCoordinateSystemManager.Instance.ParticipantConnected += OnParticipantConnected;

                        if (!ShouldUseNetworkConfigurationVisual())
                        {
                            DebugLog("Not using a network configuration visual, beginning state synchronization as an observer.");
                            RunStateSynchronizationAsObserver();
                        }
                        else
                        {
                            DebugLog("Using a network configuration visual. State synchronization will be delayed until a connection is started by the user.");
                            SetupNetworkConfigurationVisual();
                        }
                    }
                    break;
            }

            SetupRecordingService();
        }

        private void Update()
        {
            if (hideDevelopmentConsole &&
                Debug.developerConsoleVisible)
            {
                Debug.developerConsoleVisible = false;
            }
        }

        private void OnDestroy()
        {
            Destroy(settingsGameObject);

#if UNITY_ANDROID || UNITY_IOS
            Destroy(mobileRecordingServiceVisual);

            if (mobileNetworkConfigurationVisual != null)
            {
                Destroy(mobileNetworkConfigurationVisual);
                mobileNetworkConfigurationVisual = null;
                networkConfigurationVisual = null;
            }
#endif

            SpatialCoordinateSystemManager.Instance.ParticipantConnected -= OnParticipantConnected;
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

                recordingServiceVisual = mobileRecordingServiceVisual.GetComponentInChildren<IRecordingServiceVisual>(true);
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

        private void SetupNetworkConfigurationVisual()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (mobileNetworkConfigurationVisual == null)
            {
                if (networkConfigurationVisual != null)
                {
                    networkConfigurationVisual.NetworkConfigurationUpdated -= OnNetworkConfigurationUpdated;
                    networkConfigurationVisual = null;
                }

                GameObject mobileNetworkConfigurationVisualPrefab = defaultMobileNetworkConfigurationVisualPrefab;
                if (NetworkConfigurationSettings.IsInitialized && NetworkConfigurationSettings.Instance.OverrideMobileNetworkConfigurationVisualPrefab != null)
                {
                    mobileNetworkConfigurationVisualPrefab = NetworkConfigurationSettings.Instance.OverrideMobileNetworkConfigurationVisualPrefab;
                }

                mobileNetworkConfigurationVisual = Instantiate(mobileNetworkConfigurationVisualPrefab);
                networkConfigurationVisual = mobileNetworkConfigurationVisual.GetComponentInChildren<INetworkConfigurationVisual>(true);
                if (networkConfigurationVisual == null)
                {
                    Debug.LogError("Network configuration visual was not found. No connection will be established. Visual will be destroyed.");
                    Destroy(mobileNetworkConfigurationVisual);
                    mobileNetworkConfigurationVisual = null;
                }
                else
                {
                    networkConfigurationVisual.NetworkConfigurationUpdated += OnNetworkConfigurationUpdated;
                }
            }

            if (networkConfigurationVisual != null)
            {
                networkConfigurationVisual.Show();
            }
#endif
        }


        private void OnNetworkConfigurationUpdated(object sender, string ipAddress)
        {
#if UNITY_ANDROID || UNITY_IOS
            this.userIpAddress = ipAddress;
            if (networkConfigurationVisual != null)
            {
                networkConfigurationVisual.NetworkConfigurationUpdated -= OnNetworkConfigurationUpdated;
                networkConfigurationVisual.Hide();
            }

            RunStateSynchronizationAsObserver();
#endif
        }


        private bool ShouldUseNetworkConfigurationVisual()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (NetworkConfigurationSettings.IsInitialized)
            {
                return NetworkConfigurationSettings.Instance.EnableMobileNetworkConfigurationVisual;
            }
            else
            {
                return (defaultMobileNetworkConfigurationVisualPrefab != null);
            }
#else
            return false;
#endif
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                UnityEngine.Debug.Log($"SpatialLocalizationInitializationSettings: {message}");
            }
        }

        private async void OnParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Waiting for the set of supported localizers from connected participant {participant.SocketEndpoint.Address}");

            // When a remote participant connects, get the set of ISpatialLocalizers that peer
            // supports. This is asynchronous, as it comes across the network.
            ISet<Guid> peerSupportedLocalizers = await participant.GetPeerSupportedLocalizersAsync();

            // If there are any supported localizers, find the first configured localizer in the
            // list that supports that type. If and when one is found, use it to perform localization.
            if (peerSupportedLocalizers != null)
            {
                DebugLog($"Received a set of {peerSupportedLocalizers.Count} supported localizers");
                
                if (SpatialLocalizationInitializationSettings.IsInitialized &&
                    TryRunLocalization(SpatialLocalizationInitializationSettings.Instance.PrioritizedInitializers, peerSupportedLocalizers, participant))
                {
                    // Succeeded at using a custom localizer specified by the app.
                    return;
                }

                if (TryRunLocalization(defaultSpatialLocalizationInitializers, peerSupportedLocalizers, participant))
                {
                    // Succeeded at using one of the default localizers from the prefab.
                    return;
                }

                Debug.LogWarning($"None of the configured LocalizationInitializers were supported by the connected participant, localization will not be started");
            }
            else
            {
                Debug.LogWarning($"No supported localizers were received from the participant, localization will not be started");
            }
        }

        private bool TryRunLocalization(IList<SpatialLocalizationInitializer> initializers, ISet<Guid> supportedLocalizers, SpatialCoordinateSystemParticipant participant)
        {
            if (initializers == null)
            {
                return false;
            }

            for (int i = 0; i < initializers.Count; i++)
            {
                if (supportedLocalizers.Contains(initializers[i].PeerSpatialLocalizerId))
                {
                    DebugLog($"Localization initializer {initializers[i].GetType().Name} supported localization with ID {initializers[i].PeerSpatialLocalizerId}, starting localization");
                    initializers[i].RunLocalization(participant);
                    return true;
                }
            }

            return false;
        }
    }
}
