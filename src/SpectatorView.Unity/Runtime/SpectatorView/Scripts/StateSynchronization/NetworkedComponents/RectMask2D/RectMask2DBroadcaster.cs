// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class RectMask2DBroadcaster : ComponentBroadcaster<RectMask2DService, RectMask2DBroadcaster.ChangeType>
    {
        [Flags]
        public enum ChangeType : byte
        {
            None = 0x0,
            Properties = 0x1
        }

        private RectMask2D rectMask2D;
        private bool previousEnabled;

        protected override void Awake()
        {
            base.Awake();

            this.rectMask2D = GetComponent<RectMask2D>();
        }

        public static bool HasFlag(ChangeType changeType, ChangeType flag)
        {
            return (changeType & flag) == flag;
        }

        protected override bool HasChanges(ChangeType changeFlags)
        {
            return changeFlags != ChangeType.None;
        }

        protected override ChangeType CalculateDeltaChanges()
        {
            ChangeType changeType = ChangeType.None;
            bool newEnabled = rectMask2D.enabled;
            if (newEnabled != previousEnabled)
            {
                previousEnabled = newEnabled;
                changeType |= ChangeType.Properties;
            }

            return changeType;
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            previousEnabled = rectMask2D.enabled;
            SendDeltaChanges(connections, ChangeType.Properties);
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, ChangeType changeFlags)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                ComponentBroadcasterService.WriteHeader(message, this);

                message.Write((byte)changeFlags);

                if (HasFlag(changeFlags, ChangeType.Properties))
                {
                    message.Write(previousEnabled);
                }

                message.Flush();
                StateSynchronizationSceneManager.Instance.Send(connections, memoryStream.GetBuffer(), 0, memoryStream.Position);
            }
        }
    }
}
