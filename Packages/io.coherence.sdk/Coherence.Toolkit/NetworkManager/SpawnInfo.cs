// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using Connection;
    using Entities;
    using System;
    using System.ComponentModel;
    using System.Linq;
    using UnityEngine;

    /// <summary>For internal use only.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct SpawnInfo
    {
        public string assetId;
        public bool isFromGroup;
        public Vector3 position;
        public Quaternion? rotation;
        public Entity connectedEntity;
        public ClientID? clientId;
        public string uniqueId;
        public ConnectionType? connectionType;
        public ICoherenceSync prefab;
        public ICoherenceBridge bridge;

        internal ComponentUpdates ComponentUpdates;

        /// <summary>
        /// Returns the initial data that will be applied to a network instantiated entity's bindings.
        /// This method is intended to query pre-synced data during <see cref="INetworkObjectInstantiator.Instantiate"/>.
        /// It can be useful when it is necessary to know a field's value ahead of time, e.g., to identify the proper pool or asset to use when instantiating a remote entity.
        /// If multiple matches exist for the given binding name and type, the first binding in the <see cref="CoherenceSync.Bindings"/> list is returned.
        /// </summary>
        /// <param name="bindingName">The name of the binding, e.g. "position"</param>
        /// <typeparam name="T">The binding's value type, e.g. "Vector3"</typeparam>
        /// <returns>Returns the initial synced value for a given binding.</returns>
        /// <exception cref="Exception">If the binding does not exist, or isn't synced with the initial packet, an exception is thrown.</exception>
        public T GetBindingValue<T>(string bindingName)
        {
            var binding = prefab.GetBakedValueBinding<ValueBinding<T>>(bindingName);
            if (binding == null)
            {
                throw new Exception($"Could not find binding: {bindingName}");
            }

            var (_, componentData) = ComponentUpdates.Store.FirstOrDefault(kvp => kvp.Value.Data.GetType() == binding.CoherenceComponentType);
            if (componentData.Data == default)
            {
                throw new Exception($"Could not find value for binding: {bindingName}");
            }

            return binding.PeekComponentData(componentData.Data);
        }
    }
}
