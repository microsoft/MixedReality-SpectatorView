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
        /// IP address for the connected peer
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Returns true if the network connection is connected, otherwise false
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Call to set the network connection state to disconnected
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Checks whether the associated client is still active. If not, the client is disconnected.
        /// </summary>
        /// <param name="currentTime">Time to use relative to last active timestamp to determine whether to disconnect</param>
        void CheckConnectionTimeout(DateTime currentTime);

        /// <summary>
        /// Call to start enqueuing incoming messages
        /// </summary>
        /// <param name="incomingQueue">Queue used for enqueuing messages</param>
        void QueueIncomingMessages(ConcurrentQueue<IncomingMessage> incomingQueue);

        /// <summary>
        /// Call to stop enqueuing incoming messages
        /// </summary>
        void StopIncomingMessageQueue();

        /// <summary>
        /// Call to send data to the connected peer
        /// </summary>
        /// <param name="data">data to send</param>
        void Send(byte[] data);
    }
}
