// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MobileNetworkConfigurationVisual : MonoBehaviour,
        INetworkConfigurationVisual
    {
        [Tooltip("The InputField used to specify an IPAddress by the user.")]
        [SerializeField]
        private InputField ipAddressField = null;

        [Tooltip("The Button used to start a network connection by the user.")]
        [SerializeField]
        private Button connectButton = null;

        [Tooltip("Check to enable debug logging.")]
        [SerializeField]
        private bool debugLogging = false;

        private string ipAddress = "127.0.0.1";

        public event Action<INetworkConfigurationVisual, string> NetworkConfigurationUpdated;
        private readonly string ipAddressPlayerPrefKey = $"{nameof(MobileNetworkConfigurationVisual)}.{nameof(ipAddress)}";

        public void Show()
        {
            this.gameObject.SetActive(true);
        }

        public void Hide()
        {
            this.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectButtonClick);
            }

            ipAddress = PlayerPrefs.GetString(ipAddressPlayerPrefKey, ipAddress);
            ipAddressField.text = ipAddress;
        }

        private void OnDisable()
        {
            PlayerPrefs.SetString(ipAddressPlayerPrefKey, ipAddress);
            PlayerPrefs.Save();
        }

        private void OnConnectButtonClick()
        {
            DebugLog("Connect was pressed!");
            if (ipAddressField == null ||
                ipAddressField.text.Trim() == "127.0.0.1" ||
                !IPAddress.TryParse(ipAddressField.text, out var address))
            {
                DebugLog("Unable to obtain ip address from field.");
                ipAddressField.text = ipAddress;
                return;
            }

            ipAddress = address.ToString();
            NetworkConfigurationUpdated?.Invoke(this, ipAddress);
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"MobileNetworkConfigurationVisual: {message}");
            }
        }
    }
}
