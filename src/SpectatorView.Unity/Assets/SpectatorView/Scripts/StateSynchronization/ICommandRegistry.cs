// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;

namespace Microsoft.MixedReality.SpectatorView
{
    public delegate void ConnectedEventHandler(INetworkConnection connection);
    public delegate void DisconnectedEventHandler(INetworkConnection connection);
    public delegate void CommandHandler(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize);

    public interface ICommandRegistry
    {
        event ConnectedEventHandler Connected;
        event DisconnectedEventHandler Disconnected;

        void RegisterCommandHandler(string command, CommandHandler handler);
        void UnregisterCommandHandler(string command, CommandHandler handler);
    }
}
