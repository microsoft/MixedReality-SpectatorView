// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface ISpatialLocalizationSession : IDisposable
    {
        /// <summary>
        /// Participant associated with the current localization session
        /// </summary>
        IPeerConnection Peer { get; }

        /// <summary>
        /// Call to obtain a spatial coordinate from this localization session
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Call to cancel the localization sessions
        /// </summary>
        void Cancel();

        /// <summary>
        /// Call to provide network information
        /// </summary>
        /// <param name="reader">reader containing a network payload</param>
        void OnDataReceived(BinaryReader reader);
    }
}