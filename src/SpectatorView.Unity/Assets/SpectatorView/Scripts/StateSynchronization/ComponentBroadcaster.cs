// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Abstract class for sending component updates from the user device.
    /// </summary>
    public interface IComponentBroadcaster
    {
        /// <summary>
        /// The associated component service.
        /// </summary>
        IComponentBroadcasterService ComponentBroadcasterService { get; }

        /// <summary>
        /// The components transform broadcaster
        /// </summary>
        TransformBroadcaster TransformBroadcaster { get; }

        /// <summary>
        /// Call to reset the frame.
        /// </summary>
        void ResetFrame();

        /// <summary>
        /// Call to report the current state of network connections.
        /// </summary>
        void ProcessNewConnections(NetworkConnectionDelta connectionDelta);

        /// <summary>
        /// Call to signal the end of a frame.
        /// </summary>
        void OnFrameCompleted(NetworkConnectionDelta connectionDelta);
    }

    /// <summary>
    /// Abstract class for sending component updates from the user device.
    /// </summary>
    public abstract class ComponentBroadcaster<TComponentBroadcasterService, TChangeFlags> : MonoBehaviour, IComponentBroadcaster where TComponentBroadcasterService : Singleton<TComponentBroadcasterService>, IComponentBroadcasterService
    {
        protected TransformBroadcaster transformBroadcaster;
        private List<INetworkConnection> connectionsNeedingComponentCreation;
        private bool isUpdatedThisFrame;
        private bool isInitialized;
        private IDisposable perfMonitoringInstance = null;

        /// <inheritdoc />
        public TransformBroadcaster TransformBroadcaster
        {
            get { return transformBroadcaster; }
        }

        /// <inheritdoc />
        public TComponentBroadcasterService ComponentBroadcasterService
        {
            get { return Singleton<TComponentBroadcasterService>.Instance; }
        }

        /// <inheritdoc />
        IComponentBroadcasterService IComponentBroadcaster.ComponentBroadcasterService => ComponentBroadcasterService;

        /// <inheritdoc />
        protected virtual void Awake()
        {
            transformBroadcaster = GetComponent<TransformBroadcaster>();

            StateSynchronizationSceneManager.Instance.AddComponentBroadcaster(this);
        }

        protected virtual void OnDestroy(){}

        /// <inheritdoc />
        protected virtual bool UpdateWhenDisabled => false;

        public string PerformanceComponentName => this.GetType().Name;

        /// <inheritdoc />
        public virtual void ResetFrame()
        {
            isUpdatedThisFrame = false;
        }

        /// <inheritdoc />
        protected virtual bool ShouldSendChanges(INetworkConnection connection)
        {
            return TransformBroadcaster.ShouldSendTransformInHierarchy(connection);
        }

        /// <inheritdoc />
        public void ProcessNewConnections(NetworkConnectionDelta connectionDelta)
        {
            if (!isInitialized)
            {
                ProcessNewConnections(connectionDelta.AddedConnections.Concat(connectionDelta.ContinuedConnections));
            }
            else
            {
                ProcessNewConnections(connectionDelta.AddedConnections);
            }
        }

        protected virtual void ProcessNewConnections(IEnumerable<INetworkConnection> connectionsRequiringFullUpdate) { }

        /// <inheritdoc />
        public void OnFrameCompleted(NetworkConnectionDelta connectionDelta)
        {
            if (!isUpdatedThisFrame)
            {
                isUpdatedThisFrame = true;

                if (TransformBroadcaster != null && (isActiveAndEnabled || UpdateWhenDisabled))
                {
                    // Make sure the transform syncs before any other components sync
                    if (TransformBroadcaster != this)
                    {
                        TransformBroadcaster.OnFrameCompleted(connectionDelta);
                    }

                    BeginUpdatingFrame(connectionDelta);

                    // The TransformBroadcaster might detect that this component is destroyed.
                    // If so, don't continue updating.
                    if (this.enabled)
                    {
                        EnsureComponentInitialized();

                        IReadOnlyList<INetworkConnection> connectionsNeedingCompleteChanges;
                        IReadOnlyList<INetworkConnection> filteredEndpointsNeedingDeltaChanges;
                        IReadOnlyList<INetworkConnection> filteredEndpointsNeedingCompleteChanges;
                        using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "ProcessConnectionDelta"))
                        {
                            TransformBroadcaster.ProcessConnectionDelta(connectionDelta, out connectionsNeedingCompleteChanges, out filteredEndpointsNeedingDeltaChanges, out filteredEndpointsNeedingCompleteChanges);
                        }

                        if (connectionsNeedingCompleteChanges != null &&
                            connectionsNeedingCompleteChanges.Count > 0)
                        {
                            SendComponentCreation(connectionsNeedingCompleteChanges);
                        }

                        if (filteredEndpointsNeedingDeltaChanges != null &&
                            filteredEndpointsNeedingDeltaChanges.Count > 0)
                        {
                            TChangeFlags changeFlags = CalculateDeltaChanges();
                            if (HasChanges(changeFlags))
                            {
                                SendDeltaChanges(filteredEndpointsNeedingDeltaChanges, changeFlags);
                            }
                        }

                        if (filteredEndpointsNeedingCompleteChanges != null &&
                            filteredEndpointsNeedingCompleteChanges.Count > 0)
                        {
                            SendCompleteChanges(filteredEndpointsNeedingCompleteChanges);
                        }

                        if (connectionDelta.RemovedConnections != null &&
                            connectionDelta.RemovedConnections.Count > 0)
                        {
                            RemoveDisconnectedEndpoints(connectionDelta.RemovedConnections);
                        }

                        EndUpdatingFrame();
                    }
                }
            }
        }

        private void EnsureComponentInitialized()
        {
            if (!isInitialized)
            {
                OnInitialized();
                isInitialized = true;
            }
        }

        protected virtual void SendComponentCreation(IEnumerable<INetworkConnection> newConnections)
        {
            if (connectionsNeedingComponentCreation == null)
            {
                connectionsNeedingComponentCreation = new List<INetworkConnection>();
            }
            else
            {
                connectionsNeedingComponentCreation.RemoveAll(x => TrySendComponentCreation(x));
            }

            foreach (INetworkConnection newConnection in newConnections)
            {
                if (!TrySendComponentCreation(newConnection))
                {
                    connectionsNeedingComponentCreation.Add(newConnection);
                }
            }
        }

        private bool TrySendComponentCreation(INetworkConnection connection)
        {
            if (ShouldSendChanges(connection))
            {
                SendComponentCreation(connection);
                return true;
            }

            return false;
        }

        private void SendComponentCreation(INetworkConnection connection)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                this.ComponentBroadcasterService.WriteHeader(message, this, ComponentBroadcasterChangeType.Created);
                message.Flush();
                connection.Send(memoryStream.GetBuffer(), 0, memoryStream.Position);
            }
        }

        protected virtual void OnInitialized() {}

        protected virtual bool ShouldUpdateFrame(INetworkConnection connection)
        {
            return this.enabled;
        }

        protected virtual void BeginUpdatingFrame(NetworkConnectionDelta connectionDelta)
        {
            if (PerformanceComponentName != string.Empty &&
                StateSynchronizationPerformanceMonitor.Instance != null)
            {
                perfMonitoringInstance = StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(PerformanceComponentName, "FrameUpdateDuration");
            }
        }

        protected virtual void EndUpdatingFrame()
        {
            if (perfMonitoringInstance != null)
            {
                perfMonitoringInstance.Dispose();
                perfMonitoringInstance = null;
            }
        }

        protected abstract void SendCompleteChanges(IEnumerable<INetworkConnection> connections);

        protected abstract TChangeFlags CalculateDeltaChanges();

        protected abstract bool HasChanges(TChangeFlags changeFlags);

        protected abstract void SendDeltaChanges(IEnumerable<INetworkConnection> connections, TChangeFlags changeFlags);

        protected virtual void RemoveDisconnectedEndpoints(IEnumerable<INetworkConnection> connections) {}
    }
}
