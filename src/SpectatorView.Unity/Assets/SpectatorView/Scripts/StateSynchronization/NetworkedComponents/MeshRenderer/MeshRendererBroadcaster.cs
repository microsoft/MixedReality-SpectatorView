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

        protected override void SendDeltaChanges(IEnumerable<SocketEndpoint> endpoints, byte changeFlags)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendDeltaChanges"))
            {
                base.SendDeltaChanges(endpoints, changeFlags);
            }
        }

        protected override void SendCompleteChanges(IEnumerable<SocketEndpoint> endpoints)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendCompleteChanges"))
            {
                base.SendCompleteChanges(endpoints);
            }
        }

        protected override void SendComponentCreation(IEnumerable<SocketEndpoint> newConnections)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "SendComponentCreation"))
            {
                base.SendComponentCreation(newConnections);
            }
        }

        protected override bool ShouldSendChanges(SocketEndpoint endpoint)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "ShouldSendChanges"))
            {
                return base.ShouldSendChanges(endpoint);
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
