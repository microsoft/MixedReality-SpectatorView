// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Classes that implement this interface represent network connections and allow sending messages across a network.
    /// </summary>
    public interface INetworkConnection
    {
        /// <summary>
        /// Returns true if the network connection is connected, otherwise false
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Call to set the network connection state to disconnected
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Tries to subscribe to incoming messages for the connection.
        /// </summary>
        /// <param name="incomingQueue"></param>
        /// <returns> Returns true when subscribing succeeds, otherwise false.</returns>
        bool TrySubscribeToIncomingMessages(ConcurrentQueue<IncomingMessage> incomingQueue);

        /// <summary>
        /// Call to stop receiving incoming messages
        /// </summary>
        void UnsubscribeToIncomingMessages(ConcurrentQueue<IncomingMessage> incomingQueue);

        /// <summary>
        /// Call to send data to the connected peer
        /// </summary>
        /// <param name="data">data to send</param>
        void Send(byte[] data);

        /// <summary>
        /// Returns a string that can be used to identify the network connection in UI/for logging.
        /// </summary>
        string ToString();
    }
}
