using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface INetworkManager : ICommandRegistry
    {
        /// <summary>
        /// Readonly list of all current network connections.
        /// </summary>
        IReadOnlyList<INetworkConnection> Connections { get; }

        /// <summary>
        /// Gets whether or not a network connection to the device is established.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether or not a network connection to the device is pending.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Gets the local IP address reported by the socket used to connect to the device.
        /// </summary>
        string ConnectedIPAddress { get; }

        /// <summary>
        /// Gets the time since this network manager last received an update.
        /// </summary>
        TimeSpan TimeSinceLastUpdate { get; }

        /// <summary>
        /// Connect to a remote device on the default port for this network manager.
        /// </summary>
        /// <param name="targetIpString">The IP address of the device to connect to.</param>
        void ConnectTo(string targetIpString);

        /// <summary>
        /// Connect to a remote device using the specified port.
        /// </summary>
        /// <param name="targetIpString">The IP address of the device to connect to.</param>
        /// <param name="port">The port to use for communication.</param>
        void ConnectTo(string targetIpString, int port);

        /// <summary>
        /// Disconnects any active network connections to other devices.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Send a packet of data to all connected devices.
        /// </summary>
        /// <param name="data">The data to send to each connected device.</param>
        void Broadcast(byte[] data);
    }
}