// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    internal class StringGuid : IComparable
    {
        [SerializeField]
        private string m_storage;

        public static implicit operator StringGuid(Guid rhs)
        {
            return new StringGuid { m_storage = rhs.ToString("D") };
        }

        public static implicit operator Guid(StringGuid rhs)
        {
            if (rhs.m_storage == null)
            {
                return Guid.Empty;
            }

            try
            {
                return new Guid(rhs.m_storage);
            }
            catch (FormatException)
            {
                return System.Guid.Empty;
            }
        }

        public override string ToString()
        {
            return (m_storage == null) ? System.Guid.Empty.ToString("D") : m_storage;
        }

        public override int GetHashCode()
        {
            return m_storage?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        public int CompareTo(object obj)
        {
            StringGuid guid = obj as StringGuid;
            if (guid == null)
            {
                return 1;
            }

            return string.Compare(m_storage, guid.m_storage);
        }
    }
}