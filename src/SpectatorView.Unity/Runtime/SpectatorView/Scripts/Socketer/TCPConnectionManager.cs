// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Helper class for setting up a TCP based network connection
    /// </summary>
    public class TCPConnectionManager : MonoBehaviour, INetworkConnectionManager
    {
        /// <summary>
        /// If true, socket clients will attempt to reconnect when disconnected.
        /// </summary>
        [Tooltip("If true, socket clients will attempt to reconnect when disconnected.")]
        [SerializeField]
        public bool AttemptReconnectWhenClient = false;

        /// <inheritdoc />
        public event Action<INetworkConnection> OnConnected;

        /// <inheritdoc />
        public event Action<INetworkConnection> OnDisconnected;

        /// <inheritdoc />
        public event Action<IncomingMessage> OnReceive;

        private readonly ConcurrentQueue<TCPNetworkConnection> newConnections = new ConcurrentQueue<TCPNetworkConnection>();
        private readonly ConcurrentQueue<TCPNetworkConnection> oldConnections = new ConcurrentQueue<TCPNetworkConnection>();
        private readonly ConcurrentDictionary<int, TCPNetworkConnection> serverConnections = new ConcurrentDictionary<int, TCPNetworkConnection>();
        private readonly ConcurrentQueue<IncomingMessage> inputMessageQueue = new ConcurrentQueue<IncomingMessage>();
        private TCPNetworkConnection clientConnection;
        private SocketerClient client;

        private SocketerClient server;

        /// <inheritdoc />
        public IReadOnlyList<INetworkConnection> Connections
        {
            get
            {
                List<INetworkConnection> connections = new List<INetworkConnection>();
                if (clientConnection != null)
                {
                    connections.Add(clientConnection);
                }

                if (!serverConnections.IsEmpty)
                {
                    foreach(var connection in serverConnections.Values)
                    {
                        connections.Add(connection);
                    }
                }

                return connections;
            }
        }

        /// <inheritdoc />
        public bool HasConnections => (!serverConnections.IsEmpty || clientConnection != null);

        /// <inheritdoc />
        public bool IsConnecting => client != null;

        /// <inheritdoc />
        public int OutputBytesQueued => SocketerClient.OutputQueueLength;

        /// <inheritdoc />
        public void StartListening(int port)
        {
            if (server == null)
            {
                server = DoStartListening(port);
            }
        }

        private SocketerClient DoStartListening(int port)
        {
            Debug.Log("Listening on port " + port);
            SocketerClient newServer = SocketerClient.CreateListener(SocketerClient.Protocol.TCP, port);
            newServer.Connected += OnServerConnected;
            newServer.Disconnected += OnServerDisconnected;
            newServer.Start();
            return newServer;
        }

        /// <inheritdoc />
        public void StopListening()
        {
            DoStopListening(ref server);
        }

        protected void DoStopListening(ref SocketerClient listener)
        {
            if (listener != null)
            {
                Debug.Log("Stopped listening on port " + listener.Port);
                listener.Stop();
                listener = null;
            }
        }

        /// <inheritdoc />
        public void ConnectTo(string serverAddress, int port)
        {
            if (client != null)
            {
                if (client.Host == serverAddress &&
                    client.Port == port)
                {
                    Debug.Log($"Client already created: {client.Host}:{client.Port}");
                    return;
                }
                else
                {
                    Debug.Log($"Disconnecting existing client {client.Host}:{client.Port}");
                    client.Stop();
                    client.Connected -= OnClientConnected;
                    client.Disconnected -= OnClientDisconnected;
                    client = null;
                }
            }

            Debug.LogFormat($"Connecting to {serverAddress}:{port}");
            client = SocketerClient.CreateSender(SocketerClient.Protocol.TCP, serverAddress, port);
            client.Connected += OnClientConnected;
            client.Disconnected += OnClientDisconnected;
            client.Start();
        }

        private void OnServerConnected(SocketerClient client, int sourceId, string clientAddress)
        {
            Debug.Log("Server connected to " + clientAddress);
            TCPNetworkConnection connection = new TCPNetworkConnection(client, clientAddress, sourceId);
            serverConnections[sourceId] = connection;
            connection.SetIncomingMessageQueue(inputMessageQueue);
            newConnections.Enqueue(connection);
        }

        protected virtual void OnServerDisconnected(SocketerClient client, int sourceId, string clientAddress)
        {
            TCPNetworkConnection connection;
            if (serverConnections.TryRemove(sourceId, out connection))
            {
                Debug.Log("Server disconnected from " + clientAddress);
                connection.SetIncomingMessageQueue(null);
                oldConnections.Enqueue(connection);
            }
        }

        private void OnClientConnected(SocketerClient client, int sourceId, string hostAddress)
        {
            Debug.Log("Client connected to " + hostAddress);
            TCPNetworkConnection connection = new TCPNetworkConnection(client, hostAddress, sourceId);

            if (!AttemptReconnectWhenClient)
            {
                connection.StopConnectionAttempts();
            }

            clientConnection = connection;
            connection.SetIncomingMessageQueue(inputMessageQueue);
            newConnections.Enqueue(connection);
        }

        private void OnClientDisconnected(SocketerClient client, int sourceId, string hostAddress)
        {
            if (clientConnection != null)
            {
                Debug.Log("Client disconnected");
                clientConnection.SetIncomingMessageQueue(null);
                oldConnections.Enqueue(clientConnection);
                clientConnection = null;
            }

            if (!AttemptReconnectWhenClient)
            {
                Debug.Log("Stopping subscriptions to disconnected client");
                client.Stop();
                client.Connected -= OnClientConnected;
                client.Disconnected -= OnClientDisconnected;

                if (this.client == client)
                {
                    Debug.Log("Clearing client cache");
                    this.client = null;
                }
            }
        }

        private void Update()
        {
            TCPNetworkConnection connection;
            while (newConnections.TryDequeue(out connection))
            {
                OnConnected?.Invoke(connection);
            }

            while (oldConnections.TryDequeue(out connection))
            {
                OnDisconnected?.Invoke(connection);
            }

            while (inputMessageQueue.TryDequeue(out IncomingMessage resultPack))
            {
                if (resultPack == null)
                    break;

                OnReceive?.Invoke(resultPack);
            }
        }

        /// <inheritdoc />
        public void Broadcast(byte[] data, long offset, long length)
        {
            foreach (TCPNetworkConnection connection in serverConnections.Values)
            {
                connection.Send(data, offset, length);
            }

            if (clientConnection != null)
            {
                clientConnection.Send(data, offset, length);
            }

            data = null;
        }

        /// <inheritdoc />
        public void DisconnectAll()
        {
            // Make sure the client stops before attempting to disconnect
            // anything else. Otherwise, a race condition could cause the client
            // to automatically reconnect to the disconnected endpoints.
            if (client != null)
            {
                client.Stop();
                client = null;
            }

            if (clientConnection != null)
            {
                clientConnection.Disconnect();
                clientConnection = null;
            }

            foreach (TCPNetworkConnection connection in serverConnections.Values)
            {
                connection.Disconnect();
            }
            serverConnections.Clear();
        }
    }
}