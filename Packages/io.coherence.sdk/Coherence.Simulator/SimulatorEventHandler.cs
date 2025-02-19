// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using UnityEngine;
    using UnityEngine.Events;
    using Coherence.Toolkit;
    using Connection;

    [AddComponentMenu("coherence/Events/Coherence Simulator Event Handler")]
    [NonBindable]
    public class SimulatorEventHandler : CoherenceBehaviour
    {
        public bool editorIsSimulator;

        public UnityEvent onSimulatorAwake;
        public UnityEvent onClientAwake;

        public UnityEvent onSimulatorConnect;
        public UnityEvent onClientConnect;

        private IClient client;

        private void Awake()
        {
            if (CoherenceBridgeStore.TryGetBridge(gameObject.scene, out var bridge))
            {
                client = bridge.Client;
            }

#if UNITY_EDITOR
            if (SimulatorUtility.IsSimulator || editorIsSimulator)
#else
            if (SimulatorUtility.IsSimulator)
#endif
            {
                onSimulatorAwake.Invoke();
            }
            else
            {
                onClientAwake.Invoke();
            }
        }

        private void OnEnable()
        {
            if (client == null)
            {
                return;
            }

            client.OnConnected += OnConnected;
        }

        private void OnDisable()
        {
            if (client == null)
            {
                return;
            }

            client.OnConnected -= OnConnected;
        }

        private void OnConnected(ClientID _)
        {
#if UNITY_EDITOR
            if (SimulatorUtility.IsSimulator || editorIsSimulator)
#else
            if (SimulatorUtility.IsSimulator)
#endif
            {
                onSimulatorConnect.Invoke();
            }
            else
            {
                onClientConnect.Invoke();
            }
        }
    }
}
