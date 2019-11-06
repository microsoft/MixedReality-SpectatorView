// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class DeviceInfoObserver : MonoBehaviour
    {
        private static readonly TimeSpan trackingStalledReceiveDelay = TimeSpan.FromSeconds(1.0);

        public const string CreateSharedSpatialCoordinateCommand = "CreateSharedSpatialCoordinate";
        public const string DeviceInfoCommand = "DeviceInfo";
        public const string StatusCommand = "Status";

        private INetworkManager networkManager;
        private INetworkConnection networkConnection;
        private string deviceName;
        private string deviceIPAddress;

        /// <summary>
        /// Gets the network manager associated with the device.
        /// </summary>
        public INetworkManager NetworkManager => networkManager;

        /// <summary>
        /// Gets the INetworkConnection for the currently-connected device.
        /// </summary>
        public INetworkConnection NetworkConnection => networkConnection;

        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        public string DeviceName => deviceName;

        /// <summary>
        /// Gets the IP address reported by the device itself.
        /// </summary>
        public string DeviceIPAddress => deviceIPAddress;

        /// <summary>
        /// Gets whether or not the receipt of new poses from the device has stalled for an unexpectedly-large time.
        /// </summary>
        public bool IsTrackingStalled => networkManager.IsConnected && networkManager.TimeSinceLastUpdate > trackingStalledReceiveDelay;

        private void Awake()
        {
            networkManager = GetComponent<INetworkManager>();
            if (networkManager == null)
            {
                throw new MissingComponentException("Missing network manager component");
            }

            networkManager.Connected += OnConnected;
            networkManager.Disconnected += OnDisconnected;
            networkManager.RegisterCommandHandler(DeviceInfoCommand, HandleDeviceInfoCommand);

            if (networkManager.IsConnected)
            {
                var connections = networkManager.Connections;
                if (connections.Count > 1)
                {
                    Debug.LogWarning("More than one connection was found, DeviceInfoObserver only expects one network connection");
                }

                foreach (var connection in connections)
                {
                    OnConnected(connection);
                }
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.Connected -= OnConnected;
                networkManager.Disconnected -= OnDisconnected;
                networkManager.UnregisterCommandHandler(DeviceInfoCommand, HandleDeviceInfoCommand);
            }
        }

        private void OnConnected(INetworkConnection connection)
        {
            networkConnection = connection;
        }

        private void OnDisconnected(INetworkConnection connection)
        {
            if (networkConnection == connection)
            {
                networkConnection = null;
            }
        }

        private void HandleDeviceInfoCommand(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            deviceName = reader.ReadString();
            deviceIPAddress = reader.ReadString();
        }
    }
}