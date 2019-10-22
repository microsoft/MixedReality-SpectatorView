// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface INetworkConnection
    {
        string Address { get; }
        bool IsConnected { get; }
        void Disconnect();
        void CheckConnectionTimeout(DateTime currentTime);
        void QueueIncomingMessages(ConcurrentQueue<IncomingMessage> incomingQueue);
        void StopIncomingMessageQueue();
        void Send(byte[] data);
    }
}
