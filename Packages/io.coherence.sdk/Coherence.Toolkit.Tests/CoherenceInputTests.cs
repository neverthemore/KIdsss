﻿// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;
    using UnityEngine;
    using static CoherenceSync;

    /// <summary>
    /// Unit tests for <see cref="CoherenceInput"/>.
    /// </summary>
    public sealed class CoherenceInputTests : CoherenceTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ProcessAutoRequestingAuthority_Auto_Requests_Authorship_Using_Adopt_If_Orphan_And_RequestAuthority_If_Not(bool isOrphaned)
        {
            var mockSync = CreateCoherenceSyncMock(isOrphaned);
            var mockBridge = CreateCoherenceBridgeMock();
            var input = CreateCoherenceInput(mockSync.Object, mockBridge.Object);

            input.ProcessAutoRequestingAuthority();

            mockSync.Verify(sync => sync.Adopt(), isOrphaned ? Times.Once : Times.Never);
            mockSync.Verify(sync => sync.RequestAuthority(AuthorityType.State), isOrphaned ? Times.Never : Times.Once);
        }

        private CoherenceInput CreateCoherenceInput(ICoherenceSync sync, ICoherenceBridge bridge)
        {
            var result = new GameObject(nameof(CoherenceInput)).AddComponent<CoherenceInput>();
            result.Setup(sync, bridge);
            result.SetAutoRequestingAuthority();
            return result;
        }

        private Mock<ICoherenceSync> CreateCoherenceSyncMock(bool isOrphaned) => new MockSyncBuilder()
            .SetIsOrphaned(isOrphaned)
            .SetUseReflection(false)
            .SetAuthorityType(AuthorityType.Input)
            .SetSimulationTypeConfig(SimulationType.ServerSideWithClientInput)
            .SetAdoptReturns(true)
            .SetRequestAuthorityReturns(AuthorityType.State, true)
            .Build();

        private Mock<ICoherenceBridge> CreateCoherenceBridgeMock() => new MockBridgeBuilder().SetIsSimulatorOrHost(true).Build();
    }
}
