// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public abstract class NetworkManager<TService> : CommandRegistry<TService>, INetworkManager where TService : Singleton<TService>
    {
        [Tooltip("Default prefab for creating an INetworkConnectionManager.")]
        [SerializeField]
        private GameObject defaultConnectionManagerPrefab = null;
        private GameObject connectionManagerGameObject = null;
        protected INetworkConnectionManager connectionManager = null;

        private float lastReceivedUpdate;
        private INetworkConnection currentConnection;

        /// <inheritdoc />
        public string ConnectedIPAddress => currentConnection?.ToString();

        /// <inheritdoc />
        public IReadOnlyList<INetworkConnection> Connections => connectionManager == null ? Array.Empty<INetworkConnection>() : connectionManager.Connections;

        /// <inheritdoc />
        public bool IsConnected => connectionManager != null && connectionManager.HasConnections;

        /// <inheritdoc />
        public bool IsConnecting => connectionManager != null && connectionManager.IsConnecting && !connectionManager.HasConnections;

        /// <inheritdoc />
        public TimeSpan TimeSinceLastUpdate => TimeSpan.FromSeconds(Time.time - lastReceivedUpdate);

        /// <summary>
        /// Gets the port used to connect to the remote device.
        /// </summary>
        protected abstract int RemotePort { get; }

        /// <summary>
        /// Starts a listening socket on the given port.
        /// </summary>
        /// <param name="port">The port to listen for new connections on.</param>
        public void StartListening(int port)
        {
            CreateConnectionManager();
            connectionManager.StartListening(port);
        }

        /// <inheritdoc />
        public void ConnectTo(string remoteAddress)
        {
            ConnectTo(remoteAddress, RemotePort);
        }

        /// <inheritdoc />
        public void ConnectTo(string ipAddress, int port)
        {
            CreateConnectionManager();
            connectionManager.ConnectTo(ipAddress, port);
        }

        /// <inheritdoc />
        public void Broadcast(byte[] data, long offset, long length)
        {
            if (currentConnection != null)
            {
                currentConnection.Send(data, offset, length);
            }
        }

        /// <summary>
        /// Disconnects the network connection to the holographic camera rig.
        /// </summary>
        public void Disconnect()
        {
            if (connectionManager != null)
            {
                connectionManager.DisconnectAll();
            }
            else
            {
                Debug.LogError($"Failed to disconnect: {nameof(connectionManager)} was not assigned.");
            }
        }

        protected override void Awake()
        {
            base.Awake();
            CreateConnectionManager();

            connectionManager.OnConnected += OnConnected;
            connectionManager.OnDisconnected += OnDisconnected;
            connectionManager.OnReceive += OnReceive;
        }

        protected virtual void Start()
        {
            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.RegisterNetworkManager(this);
            }
            else
            {
                Debug.LogError("Attempted to register NetworkManager with the SpatialCoordinateSystemManager but no SpatialCoordinateSystemManager is initialized");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (connectionManager != null)
            {
                connectionManager.StopListening();
                connectionManager.DisconnectAll();

                connectionManager.OnConnected -= OnConnected;
                connectionManager.OnDisconnected -= OnDisconnected;
                connectionManager.OnReceive -= OnReceive;

                connectionManager = null;
            }

            if (connectionManagerGameObject != null)
            {
                Destroy(connectionManagerGameObject);
            }

            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.UnregisterNetworkManager(this);
            }
        }

        protected virtual void OnConnected(INetworkConnection connection)
        {
            currentConnection = connection;

            NotifyConnected(connection);
        }

        protected virtual void OnDisconnected(INetworkConnection connection)
        {
            if (currentConnection == connection)
            {
                currentConnection = null;
            }

            NotifyDisconnected(connection);
        }

        protected void OnReceive(IncomingMessage data)
        {
            lastReceivedUpdate = Time.time;

            using (MemoryStream stream = new MemoryStream(data.Data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string command = reader.ReadString();

                NotifyCommand(data.Connection, command, reader, data.Size - (int)stream.Position);
            }
        }

        private void CreateConnectionManager()
        {
            if (connectionManagerGameObject == null)
            {
                var prefab = defaultConnectionManagerPrefab;
                if (NetworkConfigurationSettings.IsInitialized &&
                    NetworkConfigurationSettings.Instance.OverrideConnectionManagerPrefab != null)
                {
                    prefab = NetworkConfigurationSettings.Instance.OverrideConnectionManagerPrefab;
                }

                if (prefab == null)
                {
                    throw new MissingComponentException("Network connection manager prefab wasn't specified. NetworkManager will not work correctly.");
                }

                connectionManagerGameObject = Instantiate(prefab, this.transform);
                connectionManager = connectionManagerGameObject.GetComponentInChildren<INetworkConnectionManager>();

                if (connectionManager == null)
                {
                    throw new MissingComponentException("INetworkConnectionManager wasn't found in instantiated prefab. NetworkManager will not work correctly.");
                }
            }
        }
    }
}