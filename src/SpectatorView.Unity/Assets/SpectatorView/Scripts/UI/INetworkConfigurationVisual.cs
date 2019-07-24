// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface INetworkConfigurationVisual
    {
        /// <summary>
        /// Called when the associated network configuration has been updated.
        /// </summary>
        event Action<INetworkConfigurationVisual, string> NetworkConfigurationUpdated;
    }
}
