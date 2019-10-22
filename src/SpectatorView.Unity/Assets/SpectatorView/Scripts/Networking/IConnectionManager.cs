// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface IConnectionManager
    {
        event Action<INetworkConnection> OnConnected;
        event Action<INetworkConnection> OnDisconnected;
        event Action<IncomingMessage> OnReceive;

        bool HasConnections { get; }
        bool IsConnecting { get; }
        int OutputBytesQueued { get; }

        void StartListening(int port);
        void StopListening();
        void ConnectTo(string serverAddress, int port);
        void Broadcast(byte[] data);
        void DisconnectAll();
    }
}
