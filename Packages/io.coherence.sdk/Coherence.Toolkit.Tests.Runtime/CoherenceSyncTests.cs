// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// Runtime tests for <see cref="CoherenceSync"/>.
    /// </summary>
    public sealed class CoherenceSyncTests : CoherenceTest
    {
        [Test]
        public void Invalid_PositionBinding_Becomes_Null_Or_Valid_During_Initialization()
        {
            var sync = Object.Instantiate(Resources.Load<CoherenceSync>("CoherenceSync_With_Invalid_Cached_Bindings"));
            Assert.That(sync.PositionBinding is { IsValid: false }, Is.False);
        }

        [Test]
        public void Invalid_RotationBinding_Becomes_Null_Or_Valid_During_Initialization()
        {
            var sync = Object.Instantiate(Resources.Load<CoherenceSync>("CoherenceSync_With_Invalid_Cached_Bindings"));
            Assert.That(sync.RotationBinding is { IsValid: false }, Is.False);
        }

        [Test]
        public void Invalid_ScaleBinding_Becomes_Null_Or_Valid_During_Initialization()
        {
            var sync = Object.Instantiate(Resources.Load<CoherenceSync>("CoherenceSync_With_Invalid_Cached_Bindings"));
            Assert.That(sync.ScaleBinding is { IsValid: false }, Is.False);
        }
    }
}
