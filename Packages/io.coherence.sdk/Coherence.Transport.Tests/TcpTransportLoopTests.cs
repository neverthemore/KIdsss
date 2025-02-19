// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{
    using Brook;
    using Brook.Octet;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using Stats;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ReceiveQueue = System.Collections.Concurrent.ConcurrentQueue<(byte[], Connection.ConnectionException)>;
    using BlockingReceiveQueue = System.Collections.Concurrent.BlockingCollection<(byte[], Connection.ConnectionException)>;
    using SendQueue = Common.AsyncQueue<Brook.IOutOctetStream>;
    using Coherence.Tests;

    public class TcpTransportLoopTests : CoherenceTest
    {
        private static readonly TimeSpan AsyncOperationTimeout = TimeSpan.FromSeconds(5);

        private Mock<Stream> streamMock;
        private ReceiveQueue receiveQueue;
        private SendQueue sendQueue;
        private Mock<IDisposable> clientDisposerMock;
        private Mock<IStats> statsMock;
        private CancellationTokenSource cancellationSource;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            streamMock = new Mock<Stream>();
            clientDisposerMock = new Mock<IDisposable>();
            receiveQueue = new ReceiveQueue();
            sendQueue = new SendQueue();
            statsMock = new Mock<IStats>();
            cancellationSource = new CancellationTokenSource();

            clientDisposerMock = new Mock<IDisposable>();
            clientDisposerMock.Setup(m => m.Dispose()).Callback(() =>
            {
                streamMock.Object.Dispose();
            });
        }

        [TearDown]
        public override void TearDown()
        {
            cancellationSource.Cancel();

            base.TearDown();
        }

        private TcpTransportLoop CreateTestTcpTransportLoop()
        {
            return new TcpTransportLoop(
                streamMock.Object,
                clientDisposerMock.Object,
                0,
                receiveQueue,
                sendQueue,
                statsMock.Object,
                new UnityLogger(),
                cancellationSource.Token
            );
        }

        [Test]
        [Ignore("Flaky")]
        public async Task Receive_Works()
        {
            // Arrange
            TcpTransportLoop loop = CreateTestTcpTransportLoop();

            MemoryStream ms = new MemoryStream();
            WritePacket("Hello", ms);
            WritePacket("World", ms);
            ms.Seek(0, SeekOrigin.Begin);

            streamMock
                .Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] data, int offset, int len, CancellationToken ct) => Task.FromResult(ms.Read(data, 0, len)));

            // Act
            var run = loop.Run();

            // Assert
            var (ok, (data, _)) = await TryTakeFromReceiveQueue(AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data), Is.EqualTo("Hello"));

            (ok, (data, _)) = await TryTakeFromReceiveQueue(AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data), Is.EqualTo("World"));

            // Cleanup
            cancellationSource.Cancel();
            await run;
        }

        [Test]
        [Ignore("Flaky")]
        public async Task Send_Works()
        {
            // Arrange
            TcpTransportLoop loop = CreateTestTcpTransportLoop();

            BlockingCollection<byte[]> sent = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            streamMock
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] data, int offset, int len, CancellationToken ct) =>
                {
                    sent.Add(data, ct);
                    return Task.FromResult(true);
                });

            // Act
            var run = loop.Run();

            sendQueue.Enqueue(OctetStreamFromData("Hello"));
            sendQueue.Enqueue(OctetStreamFromData("World"));

            // Assert
            bool ok = sent.TryTake(out _, AsyncOperationTimeout);
            Assert.That(ok, Is.True); // Magic + RoomUID

            const int Offset = (int)TcpTransportLoop.HEADER_SIZE;

            ok = sent.TryTake(out byte[] data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, Offset, data.Length - Offset), Is.EqualTo("Hello"));

            ok = sent.TryTake(out data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, Offset, data.Length - Offset), Is.EqualTo("World"));

            // Cleanup
            cancellationSource.Cancel();
            await run;
        }

        [Test]
        [Ignore("Flaky")]
        public async Task Flush_Works()
        {
            // Arrange
            TcpTransportLoop loop = CreateTestTcpTransportLoop();
            loop.FlushTimeout = TimeSpan.FromSeconds(2);

            BlockingCollection<byte[]> sent = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            streamMock
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] data, int offset, int len, CancellationToken ct) =>
                {
                    sent.Add(data, ct);
                    return Task.FromResult(true);
                });

            // Act
            var run = loop.Run();

            bool ok = sent.TryTake(out _, AsyncOperationTimeout);
            Assert.That(ok, Is.True); // Magic + RoomUID

            cancellationSource.Cancel();

            await Task.Yield();

            sendQueue.Enqueue(OctetStreamFromData("EndOf"));
            sendQueue.Enqueue(OctetStreamFromData("Transmission"));

            // Assert
            const int Offset = (int)TcpTransportLoop.HEADER_SIZE;

            ok = sent.TryTake(out byte[] data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, Offset, data.Length - Offset), Is.EqualTo("EndOf"));

            ok = sent.TryTake(out data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, Offset, data.Length - Offset), Is.EqualTo("Transmission"));

            // Cleanup
            cancellationSource.Cancel();
            await run;

            clientDisposerMock.Verify(m => m.Dispose(), Times.Once);
        }

        private async Task<(bool, (byte[], ConnectionException))> TryTakeFromReceiveQueue(TimeSpan timeout)
        {
            Stopwatch sw = Stopwatch.StartNew();
            (byte[], ConnectionException) result;

            while (!receiveQueue.TryDequeue(out result))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));

                if (sw.Elapsed >= timeout)
                {
                    return (false, result);
                }
            }

            return (true, result);
        }

        private static int WritePacket(string data, MemoryStream stream)
        {
            var packet = PacketFromData(data);
            stream.Write(packet.Array, 0, packet.Count);
            return packet.Count;
        }

        private static ArraySegment<byte> PacketFromData(string data)
        {
            var packet = OctetStreamFromData(data);
            TcpTransportLoop.WriteHeader(packet);
            return packet.Close();
        }

        private static OutOctetStream OctetStreamFromData(string data)
        {
            var packet = new OutOctetStream();
            packet.WriteOctets(Encoding.UTF8.GetBytes(data));
            return packet;
        }
    }
}
