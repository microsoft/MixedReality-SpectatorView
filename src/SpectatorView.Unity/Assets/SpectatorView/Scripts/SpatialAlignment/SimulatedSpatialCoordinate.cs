// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.SpatialAlignment;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Helper class for creating artificial spatial coordinates in the editor.
    /// </summary>
    /// <typeparam name="TKey">Type of key to use with this coordinate</typeparam>
    public class SimulatedSpatialCoordinate<TKey> : SpatialCoordinateUnityBase<TKey>
    {
        /// <summary>
        /// Constructor for creating an artificial spatial coordinate.
        /// </summary>
        /// <param name="id">Id to use</param>
        /// <param name="worldPosition">Position in world space</param>
        /// <param name="worldRotation">Rotation in world space</param>
        public SimulatedSpatialCoordinate(TKey id, Vector3 worldPosition, Quaternion worldRotation) : base(id)
        {
            SetCoordinateWorldTransform(worldPosition, worldRotation);
        }
    }
}