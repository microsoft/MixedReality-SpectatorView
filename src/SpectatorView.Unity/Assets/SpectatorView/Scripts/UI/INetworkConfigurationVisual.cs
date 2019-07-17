// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.MixedReality.SpectatorView
{
    public delegate void NetworkConfigurationUpdatedHandler(object sender, string ipAddress);

    public interface INetworkConfigurationVisual
    {
        /// <summary>
        /// Called when the associated network configuration has been updated.
        /// </summary>
        event NetworkConfigurationUpdatedHandler NetworkConfigurationUpdated;
    }
}
