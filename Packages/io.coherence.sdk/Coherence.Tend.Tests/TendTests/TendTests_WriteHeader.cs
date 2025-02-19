namespace Coherence.Tend.Tests
{
    using System;
    using Brook;
    using Brook.Octet;
    using Models;
    using Moq;
    using NUnit.Framework;

    public partial class TendTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void WriteHeader_ShouldWriteAndReturnHeader(bool isRealiable)
        {
            // Arrange
            var outStream = new OutOctetStream();
            var expectedHeader = new TendHeader
            {
                isReliable = isRealiable,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20),
            };
            var inStream = SerializeHeader(expectedHeader);

            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(true);
            outgoingLogicMock.SetupGet(o => o.OutgoingSequenceId).Returns(expectedHeader.packetId);
            incomingLogicMock.SetupGet(o => o.LastReceivedToUs).Returns(expectedHeader.receivedId);
            incomingLogicMock.SetupGet(o => o.ReceiveMask).Returns(expectedHeader.receiveMask);

            // Act
            var header = tend.WriteHeader(outStream, isRealiable);

            // Assert
            if (!isRealiable)
            {
                Assert.AreEqual(isRealiable, header.isReliable, "Returned header should be correct");
            }
            else
            {
                Assert.AreEqual(expectedHeader, header, "Returned header should be correct");
            }

            Assert.AreEqual(inStream.ReadOctets(inStream.RemainingOctetCount).ToArray(), outStream.Close(),
                "OutStream should be correct");
        }

        [Test]
        public void WriteHeader_WhenCanNotIncrement_ShouldThrow()
        {
            // Arrange
            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(false);

            // Act - Assert
            Assert.Throws<Exception>(() => tend.WriteHeader(null, true));
        }

        [Test]
        public void WriteHeader_WhenReliable_ShouldIncreaseSequenceId()
        {
            // Arrange
            var outStream = new OutOctetStream();

            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(true);

            // Act
            tend.WriteHeader(outStream, true);

            // Assert
            outgoingLogicMock.Verify(o => o.IncreaseOutgoingSequenceId(), Times.Once,
                "Should call IncreaseOutgoingSequenceId() once");
        }

        [Test]
        public void WriteHeader_WhenNotReliable_ShouldNotIncreaseSequenceId()
        {
            // Arrange
            var outStream = new OutOctetStream();

            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(true);

            // Act
            tend.WriteHeader(outStream, false);

            // Assert
            outgoingLogicMock.Verify(o => o.IncreaseOutgoingSequenceId(), Times.Never,
                "Should not call IncreaseOutgoingSequenceId() once");
        }

        [Test]
        public void WriteHeader_WhenIncreaseThrows_ShouldRevertSequenceId()
        {
            // Arrange
            var outStream = new OutOctetStream();
            var sequenceId = new SequenceId(10);

            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(true);
            outgoingLogicMock.Setup(o => o.IncreaseOutgoingSequenceId()).Throws<Exception>();
            outgoingLogicMock.SetupGet(o => o.OutgoingSequenceId).Returns(sequenceId);

            // Act
            try
            {
                tend.WriteHeader(outStream, true);
            }
            catch
            {
            }

            // Assert
            outgoingLogicMock.VerifySet(o => o.OutgoingSequenceId = sequenceId, Times.Once,
                "Should revert OutgoingSequenceId");
        }

        [Test]
        public void WriteHeader_WhenIncreaseDoesNotThrow_ShouldNotRevertSequenceId()
        {
            // Arrange
            var outStream = new OutOctetStream();
            var sequenceId = new SequenceId(10);

            outgoingLogicMock.SetupGet(o => o.CanIncrementOutgoingSequence).Returns(true);
            outgoingLogicMock.SetupGet(o => o.OutgoingSequenceId).Returns(sequenceId);

            // Act
            tend.WriteHeader(outStream, true);

            // Assert
            outgoingLogicMock.VerifySet(o => o.OutgoingSequenceId = It.IsAny<SequenceId>(), Times.Never,
                "Should not revert OutgoingSequenceId");
        }
    }
}
