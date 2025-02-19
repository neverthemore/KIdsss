// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Bindings;
    using Entities;
    using ProtocolDef;
    using System.Collections.Generic;
    using Toolkit;
    using Coherence.Log;
    using Coherence.SimulationFrame;

    // This needs to stay on the non-editor side, otherwise it can't be added to the gameobject due to the Unity limitations.
    [UnityEngine.DisallowMultipleComponent]
    internal class CoherenceSyncBakedMock : CoherenceSyncBaked
    {
        private readonly Dictionary<string, int> callCountByName = new Dictionary<string, int>();

        public int TimesCalled(string methodName)
        {
            return callCountByName.TryGetValue(methodName, out int value) ? value : 0;
        }

        public override Binding BakeValueBinding(Binding valueBinding)
        {
            return null;
        }

        public override void BakeCommandBinding(CommandBinding commandBinding, CommandsHandler commandsHandler)
        {
        }

        public override void ReceiveCommand(IEntityCommand command)
        {
            AddCall(nameof(ReceiveCommand));
        }

        public override void Initialize(Entity entityId, CoherenceBridge bridge, IClient client, CoherenceInput input, Logger logger)
        {
            AddCall(nameof(Initialize));
        }

        public override List<ICoherenceComponentData> CreateEntity(bool usesLodsAtRuntime, string archetypeName, AbsoluteSimulationFrame simFrame)
        {
            return new List<ICoherenceComponentData>() { };
        }

        public override void Dispose()
        {
        }

        private void AddCall(string methodName)
        {
            if (callCountByName.TryGetValue(methodName, out int count))
            {
                callCountByName[methodName] = count + 1;
            }
            else
            {
                callCountByName[methodName] = 1;
            }
        }
    }
}
