// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Wrapper class for announcing the current state of network connections
    /// </summary>
    public class NetworkConnectionDelta
    {
        public NetworkConnectionDelta(IReadOnlyList<INetworkConnection> addedConnections, IReadOnlyList<INetworkConnection> removedConnections, IReadOnlyList<INetworkConnection> continuedConnections)
        {
            AddedConnections = addedConnections;
            RemovedConnections = removedConnections;
            ContinuedConnections = continuedConnections;
        }

        /// <summary>
        /// Returns true if any connections exist.
        /// </summary>
        public bool HasConnections
        {
            get
            {
                return
                    ContinuedConnections.Count > 0 || 
                    AddedConnections.Count > 0 ||
                    RemovedConnections.Count > 0;
            }
        }

        /// <summary>
        /// Network connections that were newly added.
        /// </summary>
        public IReadOnlyList<INetworkConnection> AddedConnections { get; }

        /// <summary>
        /// Network connections that were recently lost.
        /// </summary>
        public IReadOnlyList<INetworkConnection> RemovedConnections { get; }

        /// <summary>
        /// Network connections that already existed.
        /// </summary>
        public IReadOnlyList<INetworkConnection> ContinuedConnections { get; }
    }
}
