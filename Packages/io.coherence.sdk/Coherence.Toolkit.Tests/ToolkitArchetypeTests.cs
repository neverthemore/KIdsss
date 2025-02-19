// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using Coherence.Tests;

    public class ToolkitArchetypeTests : CoherenceTest
    {
        [Test]
        public void Should_HaveEmptyList_When_InstantiatingArchetype()
        {
            var archetype = new BindingArchetypeData(SchemaType.Int, typeof(int), false);

            Assert.IsTrue(archetype.Fields != null && archetype.Fields.Count == 0);
        }

        [Test]
        public void Should_HaveOneLod_When_InstantiatingArchetypeAndAddingOneLod()
        {
            var archetype = new BindingArchetypeData(SchemaType.Int, typeof(int), false);

            archetype.AddLODStep(1);

            Assert.IsTrue(archetype.Fields != null && archetype.Fields.Count == 1);
        }
    }
}

