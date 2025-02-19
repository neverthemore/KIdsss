using Coherence.SimulationFrame;
using Coherence.Toolkit.Bindings;
using NUnit.Framework;

namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;

    public class BindingTests : CoherenceTest
    {
        [Test]
        public void ComponentType_Should_UpdateComponentType_When_UpdatingGenericPoolIndex()
        {
            var binding = new MockBinding();

            Assert.IsTrue(binding.CoherenceComponentType == typeof(MockComponent0));
            binding.UpdateGenericPool(1);
            Assert.IsTrue(binding.CoherenceComponentType == typeof(MockComponent1));
        }

        internal class MockBinding : Binding
        {
            public override string CoherenceComponentName => $"MockComponent{genericPoolIndex}";
            public override string CoherenceComponentNamespace => "Coherence.Toolkit.Tests";
            public override string CoherenceComponentAssemblyName => "Coherence.Toolkit.Tests";

            public override int GetHashCode()
            {
                throw new System.NotImplementedException();
            }

            public override void IsDirty(AbsoluteSimulationFrame simulationFrame, out bool dirty, out bool justStopped)
            {
                throw new System.NotImplementedException();
            }

            public override void MarkAsReadyToSend()
            {
                throw new System.NotImplementedException();
            }
        }
    }

    public struct MockComponent0
    {

    }

    public struct MockComponent1
    {

    }
}

