// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence;
    using Connection;
    using Entities;
    using Log;
    using UnityEngine;
    using Logger = Log.Logger;

    [NonBindable]
    public abstract class CoherenceQuery : CoherenceBehaviour
    {
        /// <inheritdoc cref="CoherenceBridgeResolver{T}"/>
        public event CoherenceBridgeResolver<CoherenceQuery> BridgeResolve;

        public Entity EntityID { get; set; }
        protected IClient Client { get; private set; }
        protected Logger Logger { get; private set; }

        protected CoherenceBridge bridge;

        private bool initialized;

        private void Start()
        {
            Logger = Log.GetLogger<CoherenceQuery>(gameObject.scene);

            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, BridgeResolve, this, out bridge))
            {
                Logger.Error(Error.ToolkitQueryMissingBridge, ("object", gameObject), ("name", name),
                    ("scene", gameObject.scene));

                enabled = false;
                return;
            }

            Client = bridge.Client;

            bridge.OnAfterFloatingOriginShifted += OnFloatingOriginShiftedInternal;

            if (Client != null)
            {
                Client.OnConnected += OnConnected;
                Client.OnDisconnected += OnDisconnected;

                initialized = true;

                if (Client.IsConnected())
                {
                    OnConnected(Client.ClientID);
                }
            }
        }

        private void OnConnected(ClientID _)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var coherenceSync = GetComponentInParent<CoherenceSync>();
            if (coherenceSync != null)
            {
                if (coherenceSync.EntityState != null)
                {
                    EntityID = coherenceSync.EntityState.EntityID;
                }
            }

            CreateQuery();
        }

        private void OnDisconnected(ConnectionCloseReason _) => EntityID = Entity.InvalidRelative;

        private void Update()
        {
            if (!initialized || !Client.IsConnected())
            {
                return;
            }

            if (NeedsUpdate)
            {
                UpdateQuery();
            }
        }

        private void OnEnable()
        {
            if (!initialized || !Client.IsConnected())
            {
                return;
            }

            UpdateQuery();
        }

        private void OnDisable()
        {
            if (!initialized || !Client.IsConnected())
            {
                return;
            }

            UpdateQuery(false);
        }

        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            bridge.OnAfterFloatingOriginShifted -= OnFloatingOriginShiftedInternal;
            Client.OnConnected -= OnConnected;
            Client.OnDisconnected -= OnDisconnected;
        }

        private void OnFloatingOriginShiftedInternal(FloatingOriginShiftArgs args)
        {
            if (!initialized || !Client.IsConnected())
            {
                return;
            }

            OnFloatingOriginShifted(args);
        }

        protected virtual void OnFloatingOriginShifted(FloatingOriginShiftArgs args) { }
        protected abstract void CreateQuery();
        protected abstract bool NeedsUpdate { get; }
        protected abstract void UpdateQuery(bool queryActive = true);
    }
}
