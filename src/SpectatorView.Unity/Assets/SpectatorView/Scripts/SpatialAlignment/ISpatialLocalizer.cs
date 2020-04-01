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
    public interface IPeerConnection
    {
        void SendData(Action<BinaryWriter> writeCallback);
    }

    public interface ISpatialLocalizer
    {
        Guid SpatialLocalizerId { get; }

        string DisplayName { get; }

        ISpatialLocalizationSettings CreateDefaultSettings();

        bool TryDeserializeSettings(BinaryReader reader, out ISpatialLocalizationSettings settings);

        bool TryCreateLocalizationSession(IPeerConnection peerConnection, ISpatialLocalizationSettings settings, out ISpatialLocalizationSession session);
    }
}