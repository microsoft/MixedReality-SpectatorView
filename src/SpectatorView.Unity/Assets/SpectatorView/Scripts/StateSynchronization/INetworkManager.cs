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
        /// Starts listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen for new connections on.</param>
        void StartListening(int port);

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
        /// <param name="data">A reference to the data to send</param>
        /// <param name="offset">The offset from the start of the array to use to obtain the data to send</param>
        /// <param name="length">The length of the data to send</param>
        void Broadcast(byte[] data, long offset, long length);
    }
}