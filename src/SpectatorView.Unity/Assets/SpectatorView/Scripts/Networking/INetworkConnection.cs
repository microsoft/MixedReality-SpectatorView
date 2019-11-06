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

        /// <summary>
        /// Returns a string that can be used to identify the network connection in UI/for logging.
        /// </summary>
        string ToString();
    }
}
