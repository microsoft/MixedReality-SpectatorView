// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class CanvasBroadcaster : ComponentBroadcaster<CanvasService, CanvasBroadcaster.ChangeType>
    {
        [Flags]
        public enum ChangeType : byte
        {
            None = 0x0,
            Enabled = 0x1,
            Properties = 0x2,
        }

        private Canvas canvas;
        private bool previousEnabled;
        private CanvasProperties previousProperties;

        protected override void Awake()
        {
            base.Awake();

            canvas = GetComponent<Canvas>();
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

            if (previousEnabled != canvas.enabled)
            {
                previousEnabled = canvas.enabled;
                changeType |= ChangeType.Enabled;
            }

            var newProperties = new CanvasProperties(canvas);
            if (previousProperties != newProperties)
            {
                previousProperties = newProperties;
                changeType |= ChangeType.Properties;
            }

            return changeType;
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            previousEnabled = canvas.enabled;
            previousProperties = new CanvasProperties(canvas);
            SendDeltaChanges(connections, ChangeType.Enabled | ChangeType.Properties);
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, ChangeType changeType)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                ComponentBroadcasterService.WriteHeader(message, this);

                message.Write((byte)changeType);

                if (HasFlag(changeType, ChangeType.Enabled))
                {
                    message.Write(previousEnabled);
                }

                if (HasFlag(changeType, ChangeType.Properties))
                {
                    message.Write((byte)previousProperties.renderMode);
                    message.Write(previousProperties.sortingLayerID);
                    message.Write(previousProperties.sortingOrder);
                    message.Write(previousProperties.overrideSorting);
                }

                message.Flush();
                StateSynchronizationSceneManager.Instance.Send(connections, memoryStream.GetBuffer(), 0, memoryStream.Position);
            }
        }

        private struct CanvasProperties
        {
            public CanvasProperties(Canvas canvas)
            {
                renderMode = canvas.renderMode;
                sortingLayerID = canvas.sortingLayerID;
                sortingOrder = canvas.sortingOrder;
                overrideSorting = canvas.overrideSorting;
            }

            public RenderMode renderMode;
            public int sortingLayerID;
            public int sortingOrder;
            public bool overrideSorting;

            public static bool operator ==(CanvasProperties first, CanvasProperties second)
            {
                return first.Equals(second);
            }

            public static bool operator !=(CanvasProperties first, CanvasProperties second)
            {
                return !first.Equals(second);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CanvasProperties))
                {
                    return false;
                }

                CanvasProperties other = (CanvasProperties)obj;
                return
                    other.renderMode == renderMode &&
                    other.sortingLayerID == sortingLayerID &&
                    other.sortingOrder == sortingOrder &&
                    other.overrideSorting == overrideSorting;
            }

            public override int GetHashCode()
            {
                return renderMode.GetHashCode();
            }
        }
    }
}
