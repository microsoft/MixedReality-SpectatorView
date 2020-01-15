// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface INetworkConnectionManager
    {
        /// <summary>
        /// Called when a client or server connection is established
        /// </summary>
        event Action<INetworkConnection> OnConnected;

        /// <summary>
        /// Called when a client or server connection is disconnected
        /// </summary>
        event Action<INetworkConnection> OnDisconnected;

        /// <summary>
        /// Called when a data payload is received
        /// </summary>
        event Action<IncomingMessage> OnReceive;

        /// <summary>
        ///  Returns true if connections exist, otherwise false
        /// </summary>
        bool HasConnections { get; }

        /// <summary>
        /// Readonly list of all current connections
        /// </summary>
        IReadOnlyList<INetworkConnection> Connections { get; }

        /// <summary>
        /// Returns true if a connection is being attempted, otherwise false
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Returns the number of bytes currently queued for the server
        /// </summary>
        int OutputBytesQueued { get; }

        /// <summary>
        /// Call to begin acting as a server listening on the provided port
        /// </summary>
        /// <param name="port">port to listen on</param>
        void StartListening(int port);

        /// <summary>
        /// Call to stop acting as a server
        /// </summary>
        void StopListening();

        /// <summary>
        /// Call to start acting as a client connected to the provided server and port
        /// </summary>
        /// <param name="serverAddress">server to connect to</param>
        /// <param name="port">port to use for communication</param>
        void ConnectTo(string serverAddress, int port);

        /// <summary>
        /// Call to broadcast the provided data to all connected clients/servers
        /// </summary>
        /// <param name="data">A reference to the data to send</param>
        /// <param name="offset">The offset from the start of the array to use to obtain the data to send</param>
        /// <param name="length">The length of the data to send</param>
        void Broadcast(byte[] data, long offset, long length);

        /// <summary>
        /// Disconnect all connections
        /// </summary>
        void DisconnectAll();
    }
}
