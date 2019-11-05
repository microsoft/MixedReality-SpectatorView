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

        private readonly TimeSpan timeoutInterval = TimeSpan.Zero;
        private readonly ConcurrentQueue<TCPSocketEndpoint> newConnections = new ConcurrentQueue<TCPSocketEndpoint>();
        private readonly ConcurrentQueue<TCPSocketEndpoint> oldConnections = new ConcurrentQueue<TCPSocketEndpoint>();
        private readonly ConcurrentDictionary<int, TCPSocketEndpoint> serverConnections = new ConcurrentDictionary<int, TCPSocketEndpoint>();
        private readonly ConcurrentQueue<IncomingMessage> inputMessageQueue = new ConcurrentQueue<IncomingMessage>();
        private TCPSocketEndpoint clientConnection;
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

                if (serverConnections.Count > 0)
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
        public bool HasConnections => (serverConnections.Count > 0 || clientConnection != null);

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
            TCPSocketEndpoint socketEndpoint = new TCPSocketEndpoint(client, timeoutInterval, clientAddress, sourceId);
            serverConnections[sourceId] = socketEndpoint;
            socketEndpoint.QueueIncomingMessages(inputMessageQueue);
            newConnections.Enqueue(socketEndpoint);
        }

        protected virtual void OnServerDisconnected(SocketerClient client, int sourceId, string clientAddress)
        {
            TCPSocketEndpoint socketEndpoint;
            if (serverConnections.TryRemove(sourceId, out socketEndpoint))
            {
                Debug.Log("Server disconnected from " + clientAddress);
                socketEndpoint.StopIncomingMessageQueue();
                oldConnections.Enqueue(socketEndpoint);
            }
        }

        private void OnClientConnected(SocketerClient client, int sourceId, string hostAddress)
        {
            Debug.Log("Client connected to " + hostAddress);
            TCPSocketEndpoint socketEndpoint = new TCPSocketEndpoint(client, timeoutInterval, hostAddress, sourceId);

            if (!AttemptReconnectWhenClient)
            {
                socketEndpoint.StopConnectionAttempts();
            }

            clientConnection = socketEndpoint;
            socketEndpoint.QueueIncomingMessages(inputMessageQueue);
            newConnections.Enqueue(socketEndpoint);
        }

        private void OnClientDisconnected(SocketerClient client, int sourceId, string hostAddress)
        {
            if (clientConnection != null)
            {
                Debug.Log("Client disconnected");
                clientConnection.StopIncomingMessageQueue();
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
            DateTime utcNow = DateTime.UtcNow;
            if (clientConnection != null)
            {
                clientConnection.CheckConnectionTimeout(utcNow);
            }

            foreach (INetworkConnection endpoint in serverConnections.Values)
            {
                endpoint.CheckConnectionTimeout(utcNow);
            }

            TCPSocketEndpoint connection;
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
        public void Broadcast(byte[] data)
        {
            foreach (TCPSocketEndpoint endpoint in serverConnections.Values)
            {
                endpoint.Send(data);
            }

            if (clientConnection != null)
            {
                clientConnection.Send(data);
            }
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

            foreach (TCPSocketEndpoint endpoint in serverConnections.Values)
            {
                endpoint.Disconnect();
            }
            serverConnections.Clear();
        }
    }
}