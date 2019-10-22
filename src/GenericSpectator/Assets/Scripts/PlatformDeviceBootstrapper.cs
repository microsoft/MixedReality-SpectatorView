// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class PlatformDeviceBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// The device prefab for Windows platforms.
        /// </summary>
        [Tooltip("The device prefab for Windows platforms.")]
        [SerializeField]
        protected GameObject windowsDevicePrefab;

        /// <summary>
        /// The device prefab for the Android platform.
        /// </summary>
        [Tooltip("The device prefab for the Android platform.")]
        [SerializeField]
        protected GameObject androidDevicePrefab;

        /// <summary>
        /// The device prefab for the iOS platform.
        /// </summary>
        [Tooltip("The device prefab for the iOS platform.")]
        [SerializeField]
        protected GameObject iOSDevicePrefab;

        protected void Awake()
        {
            GameObject devicePrefab;

#if UNITY_WSA || UNITY_STANDALONE_WIN
            devicePrefab = windowsDevicePrefab;
#elif UNITY_ANDROID
            devicePrefab = androidDevicePrefab;
#elif UNITY_IOS
            devicePrefab = iOSDevicePrefab;
#else
            Debug.LogError($"There is no device prefab for the current build platform: {Application.platform}.  Please select a different build platform or add a device prefab for this one.", this);
            return;
#endif

            if (devicePrefab == null)
            {
                Debug.LogError($"The device prefab isn't set for the current build platform. Please select a different build platform or set the device prefab.", this);
                return;
            }

            Instantiate(devicePrefab, transform);
        }
    }
}
