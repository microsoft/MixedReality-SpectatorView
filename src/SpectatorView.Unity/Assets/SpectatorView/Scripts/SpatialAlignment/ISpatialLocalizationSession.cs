// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public interface ISpatialLocalizationSession : IDisposable
    {
        Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken);

        void OnDataReceived(BinaryReader reader);
    }
}