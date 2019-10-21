// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal abstract class MeshRendererBroadcaster<TComponentService> : RendererBroadcaster<MeshRenderer, TComponentService>
        where TComponentService : Singleton<TComponentService>, IComponentBroadcasterService
    {
        protected override byte InitialChangeType
        {
            get
            {
                return ChangeType.Enabled | ChangeType.Materials;
            }
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, byte changeFlags)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendDeltaChanges"))
            {
                base.SendDeltaChanges(connections, changeFlags);
            }
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendCompleteChanges"))
            {
                base.SendCompleteChanges(connections);
            }
        }

        protected override void SendComponentCreation(IEnumerable<INetworkConnection> newConnections)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendComponentCreation"))
            {
                base.SendComponentCreation(newConnections);
            }
        }

        protected override bool ShouldSendChanges(INetworkConnection connection)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "ShouldSendChanges"))
            {
                return base.ShouldSendChanges(connection);
            }
        }

        protected override byte CalculateDeltaChanges()
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "CalculateDeltaChanges"))
            {
                return base.CalculateDeltaChanges();
            }
        }
    }
}
