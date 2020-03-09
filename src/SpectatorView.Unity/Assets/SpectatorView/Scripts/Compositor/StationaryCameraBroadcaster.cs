// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class StationaryCameraBroadcaster : NetworkManager<StationaryCameraBroadcaster>
    {
        [SerializeField]
        [Tooltip("The port that the listening socket should be bound to.")]
        private int listeningPort = 7502;

        protected override int RemotePort => listeningPort;

        protected override void Awake()
        {
            base.Awake();
            connectionManager.StartListening(listeningPort);
        }
    }
}