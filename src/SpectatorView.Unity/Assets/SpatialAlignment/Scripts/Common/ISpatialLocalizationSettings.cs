// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;

namespace Microsoft.MixedReality.SpatialAlignment
{
    public interface ISpatialLocalizationSettings
    {
        void Serialize(BinaryWriter writer);
    }
}