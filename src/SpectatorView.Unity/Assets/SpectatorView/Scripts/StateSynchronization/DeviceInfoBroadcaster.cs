// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WSA
using UnityEngine.XR.WSA;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    public class DeviceInfoBroadcaster : MonoBehaviour
    {
#if UNITY_WSA
        private INetworkManager networkManager = null;

        private void Awake()
        {
            networkManager = GetComponent<INetworkManager>();
            if (networkManager == null)
            {
                throw new MissingComponentException("Missing network manager component");
            }

            networkManager.Connected += NetworkManagerConnected;

            if (networkManager.IsConnected)
            {
                SendDeviceInfo();
            }
        }

        private void OnDestroy()
        {
            networkManager.Connected -= NetworkManagerConnected;
        }

        private void NetworkManagerConnected(INetworkConnection obj)
        {
            SendDeviceInfo();
        }

        private void SendDeviceInfo()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                message.Write(DeviceInfoObserver.DeviceInfoCommand);
                message.Write(GetMachineName());
                message.Write(GetIPAddress());
                message.Flush();

                networkManager.Broadcast(memoryStream.GetBuffer(), 0, memoryStream.Position);
            }
        }

        private string GetMachineName()
        {
#if UNITY_EDITOR
            return System.Environment.MachineName;
#else
            HostName localName = NetworkInformation.GetHostNames().FirstOrDefault(n => n.Type == HostNameType.DomainName);
            if (localName == null)
            {
                return "UnknownDevice";
            }
            else
            {
                 return localName.RawName;
            }
#endif
        }

        private string GetIPAddress()
        {
            string ipAddress = "Unknown IP";

#if !UNITY_EDITOR && WINDOWS_UWP
            var icp = NetworkInformation.GetInternetConnectionProfile();
            if (icp != null && icp.NetworkAdapter != null)
            {
                HostName localName = NetworkInformation.GetHostNames().FirstOrDefault(n =>
                            n.Type == HostNameType.Ipv4 &&
                            n.IPInformation != null &&
                            n.IPInformation.NetworkAdapter != null &&
                            n.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);
                if (localName != null)
                    ipAddress = localName.ToString();
            }
#endif
            return ipAddress;
        }
#endif
    }
}