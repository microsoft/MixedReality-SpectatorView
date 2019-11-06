// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public sealed class TCPNetworkConnection : INetworkConnection
    {
        private enum ConnectionState
        {
            Connected,
            Disconnected
        }

        /// <inheritdoc />
        public string Address { get; private set; }

        private readonly SocketerClient socketerClient;
        private readonly int sourceId;
        private ConcurrentQueue<IncomingMessage> incomingQueue;
        private DateTime lastActiveTimestamp;
        private TimeSpan timeoutInterval;

        private ConnectionState State { get; set; } = ConnectionState.Connected;

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return State == ConnectionState.Connected; }
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            if (State != ConnectionState.Disconnected)
            {
                State = ConnectionState.Disconnected;
                this.socketerClient.Disconnect(sourceId);
            }
        }

        /// <inheritdoc />
        public void CheckConnectionTimeout(DateTime currentTime)
        {
            if (timeoutInterval != TimeSpan.Zero && currentTime - lastActiveTimestamp > timeoutInterval)
            {
                this.socketerClient.Disconnect(sourceId);
            }
        }

        public TCPNetworkConnection(SocketerClient socketerClient, TimeSpan timeoutInterval, string address, int sourceId = 0)
        {
            this.socketerClient = socketerClient;
            this.timeoutInterval = timeoutInterval;
            this.sourceId = sourceId;
            this.Address = address;
            this.lastActiveTimestamp = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public void QueueIncomingMessages(ConcurrentQueue<IncomingMessage> incomingQueue)
        {
            this.incomingQueue = incomingQueue;
            socketerClient.Message += Socket_Message;
        }

        /// <inheritdoc />
        public void StopIncomingMessageQueue()
        {
            socketerClient.Message -= Socket_Message;
            this.incomingQueue = null;
        }

        /// <summary>
        /// Call to prevent additional attempts at connecting. Note: if no connection has been established, calling this function will prevent establishing a connection.
        /// </summary>
        public void StopConnectionAttempts()
        {
            socketerClient.StopConnectionAttempts();
        }

        /// <inheritdoc />
        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Attempted to send message to disconnected TCPNetworkConnection.");
                return;
            }

            try
            {
                socketerClient.SendNetworkMessage(data, sourceId);
            }
            catch
            {
                socketerClient.Disconnect(sourceId);
            }
        }

        private void Socket_Message(SocketerClient arg1, MessageEvent e)
        {
            // This event is sent to all socket endpoints. Make sure this message matches the server (connectionId == 0) or the correct client (sourceId == e.SourceId)
            if (sourceId == 0 || sourceId == e.SourceId)
            {
                lastActiveTimestamp = DateTime.UtcNow;
                IncomingMessage incomingMessage = new IncomingMessage(this, e.Message, e.Message.Length);
                incomingQueue.Enqueue(incomingMessage);
            }
        }
    }
}
