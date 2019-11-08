// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    internal class AssetId
    {
        public static AssetId Empty { get; } = new AssetId(System.Guid.Empty, -1);

        /// <summary>
        /// The Unity guid for the Asset
        /// </summary>
        public StringGuid Guid => guid;

        [SerializeField]
        private StringGuid guid;

        /// <summary>
        /// The Unity file identifier for the Asset
        /// </summary>
        public long FileIdentifier => fileIdentifier;

        [SerializeField]
        private long fileIdentifier;

        public AssetId(StringGuid guid, long fileIdentifier)
        {
            this.guid = guid;
            this.fileIdentifier = fileIdentifier;
        }

        public override bool Equals(object obj)
        {
            AssetId assetId = obj as AssetId;
            if (assetId == null)
            {
                return false;
            }

            return this == assetId;
        }

        public override int GetHashCode()
        {
            return FileIdentifier.GetHashCode() ^ Guid.GetHashCode();
        }

        public static bool operator ==(AssetId lhs, AssetId rhs)
        {
            if ((object)lhs == null && (object)rhs == null)
            {
                return true;
            }
            else if ((object)lhs == null && (object)rhs != null ||
                (object)lhs != null && (object)rhs == null)
            {
                return false;
            }

            return Equals(lhs.Guid, rhs.Guid) && (lhs.FileIdentifier == rhs.FileIdentifier);
        }

        public static bool operator !=(AssetId lhs, AssetId rhs)
        {
            return !(lhs == rhs);
        }
    }

    internal class AssetCacheEntry<T> where T : class
    {
        [SerializeField]
        public AssetId AssetId;

        [SerializeField]
        public T Asset;
    }
}