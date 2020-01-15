// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// A ComponentBroadcaster that allows instantiating a custom child hierarchy for the remote DynamicGameObjectHierarchyObserver.
    /// The corresponding DynamicGameObjectHierarchyObserver is responsible for creating an initially-identical child
    /// hierarchy. Once both devices have created the same initial hierarchy, the hierarchies are bound together
    /// and state synchronization is initialized for all of the GameObjects within that hierarchy.
    /// </summary>
    /// <typeparam name="TComponentService">The IComponentBroadcasterService responsible for network communication for this ComponentBroadcaster.</typeparam>
    public abstract class DynamicGameObjectHierarchyBroadcaster<TComponentService> : ComponentBroadcaster<TComponentService, byte> where TComponentService : Singleton<TComponentService>, IComponentBroadcasterService
    {
        private GameObject dynamicObject;
        private Dictionary<INetworkConnection, PerConnectionInstantiationState> perConnectionInstantiationState = new Dictionary<INetworkConnection, PerConnectionInstantiationState>();

        private class PerConnectionInstantiationState
        {
            public bool observerObjectCreated;
            public bool sendInstantiationRequest;
            public bool sendTransformHierarchyBinding;
        }

        public static class ChangeType
        {
            public const byte CreateObserverObject = 0x0;
            public const byte ObserverObjectCreated = 0x1;
            public const byte BindTransformHierarchy = 0x2;
            public const byte ObserverHierarchyBound = 0x3;
        }

        /// <summary>
        /// Gets or sets the locally-created dynamic GameObject hierarchy root.
        /// </summary>
        protected GameObject DynamicObject
        {
            get { return dynamicObject; }
            set
            {
                if (dynamicObject != value)
                {
                    dynamicObject = value;

                    if (dynamicObject != null)
                    {
                        OnDynamicObjectConstructed();
                    }
                }
            }
        }

        protected override byte CalculateDeltaChanges()
        {
            return 0;
        }

        protected override bool HasChanges(byte changeFlags)
        {
            return true;
        }

        protected override bool ShouldSendChanges(INetworkConnection connection)
        {
            // We always need to send changes for dynamic components to negotiate the observer instantiation
            return true;
        }

        protected override void ProcessNewConnections(IEnumerable<INetworkConnection> connectionsRequiringFullUpdate)
        {
            foreach (INetworkConnection newConnection in connectionsRequiringFullUpdate)
            {
                if (DynamicObject == null)
                {
                    TransformBroadcaster.BlockedConnections.Add(newConnection);
                }
                else
                {
                    DynamicObject.GetComponent<TransformBroadcaster>().BlockedConnections.Add(newConnection);
                }
            }
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            foreach (INetworkConnection connection in connections)
            {
                if (!perConnectionInstantiationState.ContainsKey(connection))
                {
                    perConnectionInstantiationState.Add(connection, new PerConnectionInstantiationState
                    {
                        observerObjectCreated = false,
                        sendInstantiationRequest = true
                    });
                }
            }

            SendDeltaChanges(connections, 0);
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, byte changeFlags)
        {
            foreach (INetworkConnection connection in connections)
            {
                PerConnectionInstantiationState state;
                if (perConnectionInstantiationState.TryGetValue(connection, out state))
                {
                    if (state.sendInstantiationRequest)
                    {
                        state.sendInstantiationRequest = false;
                        using (MemoryStream memoryStream = new MemoryStream())
                        using (BinaryWriter message = new BinaryWriter(memoryStream))
                        {
                            ComponentBroadcasterService.WriteHeader(message, this);

                            message.Write((byte)ChangeType.CreateObserverObject);
                            WriteInstantiationRequestParameters(message);

                            message.Flush();
                            connection.Send(memoryStream.GetBuffer(), 0, memoryStream.Position);
                        }
                    }

                    if (state.sendTransformHierarchyBinding)
                    {
                        state.sendTransformHierarchyBinding = false;
                        SendTransformHierarchyBinding(connection);
                    }
                }
            }
        }

        protected override void RemoveDisconnectedEndpoints(IEnumerable<INetworkConnection> connections)
        {
            foreach (INetworkConnection connection in connections)
            {
                perConnectionInstantiationState.Remove(connection);
            }
        }

        protected abstract void WriteInstantiationRequestParameters(BinaryWriter message);
        
        private void OnDynamicObjectConstructed()
        {
            foreach (INetworkConnection connection in perConnectionInstantiationState.Keys)
            {
                if (TransformBroadcaster.BlockedConnections.Remove(connection))
                {
                    DynamicObject.GetComponent<TransformBroadcaster>().BlockedConnections.Add(connection);
                }
                else
                {
                    Debug.LogError("Expected that the object was previously blocked");
                }
            }

            foreach (KeyValuePair<INetworkConnection, PerConnectionInstantiationState> state in perConnectionInstantiationState)
            {
                if (state.Value.observerObjectCreated)
                {
                    state.Value.sendTransformHierarchyBinding = true;
                }
            }
        }

        protected void SendInstantiationRequest()
        {
            foreach (KeyValuePair<INetworkConnection, PerConnectionInstantiationState> state in perConnectionInstantiationState)
            {
                TransformBroadcaster.BlockedConnections.Add(state.Key);
                state.Value.observerObjectCreated = false;
                state.Value.sendInstantiationRequest = true;
            }
        }

        public void Read(INetworkConnection connection, BinaryReader message)
        {
            byte changeType = message.ReadByte();

            Read(connection, message, changeType);
        }

        protected virtual void Read(INetworkConnection connection, BinaryReader message, byte changeType)
        {
            switch (changeType)
            {
                case ChangeType.ObserverObjectCreated:
                    {
                        PerConnectionInstantiationState state;
                        if (perConnectionInstantiationState.TryGetValue(connection, out state))
                        {
                            state.observerObjectCreated = true;

                            if (DynamicObject != null)
                            {
                                state.sendTransformHierarchyBinding = true;
                            }
                        }
                    }
                    break;
                case ChangeType.ObserverHierarchyBound:
                    if (DynamicObject != null)
                    {
                        DynamicObject.GetComponent<TransformBroadcaster>().BlockedConnections.Remove(connection);
                    }
                    break;
            }
        }

        private void SendTransformHierarchyBinding(INetworkConnection connection)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                ComponentBroadcasterService.WriteHeader(message, this);

                message.Write((byte)ChangeType.BindTransformHierarchy);
                DynamicObject.GetComponent<TransformBroadcaster>().WriteChildHierarchyTree(message);

                message.Flush();

                connection.Send(memoryStream.GetBuffer(), 0, memoryStream.Position);
            }
        }
    }
}