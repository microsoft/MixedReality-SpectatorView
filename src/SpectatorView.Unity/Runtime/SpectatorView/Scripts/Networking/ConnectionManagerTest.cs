// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Helper class for testing <see cref="INetworkConnectionManager"/> prefabs
    /// </summary>
    public class ConnectionManagerTest : MonoBehaviour
    {
        /// <summary>
        /// Check to run as the server
        /// </summary>
        [Tooltip("Check to run as the server")]
        [SerializeField]
        protected bool runAsServer = false;

        /// <summary>
        /// IP address of the server
        /// </summary>
        [Tooltip("IP address of the server")]
        [SerializeField]
        private string serverAddress = "127.0.0.1";

        /// <summary>
        /// Port for communicating with the server
        /// </summary>
        [Tooltip("Port for communicating with the server")]
        [SerializeField]
        private int serverPort = 7777;

        /// <summary>
        /// Time between broadcasts
        /// </summary>
        [Tooltip("Time between broadcasts")]
        [SerializeField]
        protected float timeBetweenBroadcasts = 1.0f;

        /// <summary>
        /// INetworkConnectionManager to use for networking
        /// </summary>
        [Tooltip("Prefab that contains INetworkConnectionManager to use for networking")]
        [SerializeField]
        protected GameObject connectionManagerPrefab;
        private GameObject connectionManagerGameObject;
        private INetworkConnectionManager connectionManager;

        private float lastBroadcast = 0.0f;
        private bool broadcastSent = false;
        private bool broadcastReceived = false;

        private void OnValidate()
        {
#if UNITY_EDITOR
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClient, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
#endif
        }

        private void Start()
        {
            connectionManagerGameObject = Instantiate(connectionManagerPrefab);
            connectionManager = connectionManagerGameObject.GetComponent<INetworkConnectionManager>();

            connectionManager.OnConnected += OnNetConnected;
            connectionManager.OnDisconnected += OnNetDisconnected;
            connectionManager.OnReceive += OnNetReceived;

            if (runAsServer)
            {
                connectionManager.StartListening(serverPort);
            }
            else
            {
                connectionManager.ConnectTo(serverAddress, serverPort);
            }
        }

        private void Update()
        {
            if (connectionManager.Connections.Count == 0)
            {
                return;
            }

            if (broadcastSent &&
                broadcastReceived)
            {
                Debug.Log("Broadcasts sent and received, attempting to disconnect");
                connectionManager.DisconnectAll();
                Debug.Log("INetworkConnectionManager has disconnected");
            }
            else if ((Time.time - lastBroadcast) > timeBetweenBroadcasts)
            {
                var message = runAsServer ? "Message from server" : "Message from client";
                var data = Encoding.ASCII.GetBytes(message);
                connectionManager.Broadcast(data, 0, data.Length);
                broadcastSent = true;

                lastBroadcast = Time.time;
            }
        }

        private void OnDestroy()
        {
            connectionManager.DisconnectAll();
            connectionManager.OnConnected -= OnNetConnected;
            connectionManager.OnDisconnected -= OnNetDisconnected;
            connectionManager.OnReceive -= OnNetReceived;

            connectionManager = null;
            Destroy(connectionManagerGameObject);
            connectionManagerGameObject = null;
        }

        private void OnNetConnected(INetworkConnection obj)
        {
            Debug.Log($"INetworkConnectionManager Connected:{obj.ToString()}");
        }

        private void OnNetDisconnected(INetworkConnection obj)
        {
            Debug.Log($"INetworkConnectionManager Disconnected:{obj.ToString()}");
        }

        private void OnNetReceived(IncomingMessage obj)
        {
            Debug.Log($"INetworkConnectionManager Received:{obj.ToString()}");
            broadcastReceived = true;
        }
    }
}