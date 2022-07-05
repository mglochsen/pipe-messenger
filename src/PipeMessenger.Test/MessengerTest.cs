using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;
using Xunit;

namespace PipeMessenger.Test
{
    public class MessengerTest
    {
        [Fact]
        public async Task InitAsync_ConnectsPipe()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            var cancellationToken = new CancellationToken();
            var target = CreateMessenger(pipeStreamMock.Object);

            // Act
            await target.InitAsync(cancellationToken);

            // Assert
            pipeStreamMock.Verify(
                pipe => pipe.ConnectAsync(cancellationToken),
                Times.Once());
        }

        [Fact]
        public async Task InitAsync_InvokesConnectedAtHandler()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);

            // Act
            await target.InitAsync();

            // Assert
            handlerMock.Verify(
                handler => handler.OnConnected(),
                Times.Once());
        }

        [Fact]
        public async Task InitAsync_SubscribesToPipe()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            var target = CreateMessenger(pipeStreamMock.Object);

            // Act
            await target.InitAsync();

            // Assert
            pipeStreamMock.Verify(
                pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()),
                Times.Once());
        }

        [Fact]
        public async Task IsConnected_GetsValueFromPipe()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock.SetupGet(pipe => pipe.IsConnected).Returns(true);
            var target = CreateMessenger(pipeStreamMock.Object);
            await target.InitAsync();

            // Act
            var isConnected = target.IsConnected;

            // Assert
            isConnected.Should().BeTrue();
            pipeStreamMock.VerifyGet(
                pipe => pipe.IsConnected,
                Times.Once());
        }

        [Fact]
        public void SendAsync_ThrowsException_WhenNotConnected()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock.SetupGet(pipe => pipe.IsConnected).Returns(false);
            var payload = new byte[] { 1, 2, 3 };
            var target = CreateMessenger(pipeStreamMock.Object);

            // Act and assert
            new Func<Task>(() => target.SendAsync(payload)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SendAsync_WritesMessage()
        {
            // Arrange
            var writtenBytes = new byte[0];
            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeStreamMock
                .Setup(pipe => pipe.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Callback<byte[], CancellationToken?>((bytes, token) => writtenBytes = bytes);
            var payload = new byte[] { 1, 2, 3 };
            var target = CreateMessenger(pipeStreamMock.Object);
            await target.InitAsync();

            // Act
            await target.SendAsync(payload);

            // Assert
            pipeStreamMock.Verify(
                pipe => pipe.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()),
                Times.Once());
            writtenBytes.Should().HaveCount(17 + payload.Length);
            writtenBytes[16].Should().Be((byte)MessageType.FireAndForget);
            writtenBytes.Skip(17).Should().BeEquivalentTo(payload);
        }

        [Fact]
        public async Task SendRequestAsync_ReturnsMessageId()
        {
            // Arrange
            var writtenBytes = new byte[0];

            var requestPayload = new byte[] { 1, 2, 3 };
            var responsePayload = new byte[] { 7, 8, 9 };

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeStreamMock
                .Setup(pipe => pipe.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Callback<byte[], CancellationToken?>((bytes, token) => writtenBytes = bytes)
                .ReturnsAsync(true);

            var target = CreateMessenger(pipeStreamMock.Object);
            await target.InitAsync();

            // Act
            var requestId = await target.SendRequestAsync(requestPayload);

            // Assert
            var messageId = MessageSerializer.DeserializeMessage(writtenBytes).Id;
            requestId.Should().Be(messageId);
        }

        [Fact]
        public async Task MessengerInvokesDisconnectedAtHandler()
        {
            // Arrange
            IObserver<byte[]> pipeObserver = null;

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .Setup(pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()))
                .Callback<IObserver<byte[]>>(observer => pipeObserver = observer);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);
            await target.InitAsync();

            // Act
            pipeObserver.OnNext(null);
            pipeObserver.OnCompleted();

            // Assert
            handlerMock.Verify(handler => handler.OnDisconnected(), Times.Once());
        }

        [Fact]
        public async Task MessengerDoesNotReconnect_WhenDisabled()
        {
            // Arrange
            IObserver<byte[]> pipeObserver = null;

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .Setup(pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()))
                .Callback<IObserver<byte[]>>(observer => pipeObserver = observer);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);
            await target.InitAsync();

            // Act
            pipeObserver.OnNext(null);
            pipeObserver.OnCompleted();

            // Assert
            pipeStreamMock.Verify(pipe => pipe.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task MessengerReconnects_WhenEnabled()
        {
            // Arrange
            IObserver<byte[]> pipeObserver = null;

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .Setup(pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()))
                .Callback<IObserver<byte[]>>(observer => pipeObserver = observer);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object, true);
            await target.InitAsync();

            // Act
            pipeObserver.OnNext(null);
            pipeObserver.OnCompleted();

            // Assert
            pipeStreamMock.Verify(pipe => pipe.ConnectAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task MessengerHandlesFireAndForgetMessages()
        {
            // Arrange
            IObserver<byte[]> pipeObserver = null;
            var message = new Message(Guid.NewGuid(), MessageType.FireAndForget, new byte[] { 1, 2, 3 });
            var serializedMessage = MessageSerializer.SerializeMessage(message);

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .Setup(pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()))
                .Callback<IObserver<byte[]>>(observer => pipeObserver = observer);
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);
            await target.InitAsync();

            // Act
            pipeObserver.OnNext(serializedMessage);
            pipeObserver.OnCompleted();

            // Assert
            handlerMock.Verify(handler => handler.OnMessage(message.Payload), Times.Once());
        }

        [Fact]
        public async Task MessengerHandlesRequestMessages()
        {
            // Arrange
            IObserver<byte[]> pipeObserver = null;
            var message = new Message(Guid.NewGuid(), MessageType.Request, new byte[] { 1, 2, 3 });
            var serializedMessage = MessageSerializer.SerializeMessage(message);

            var pipeStreamMock = new Mock<IPipeStream>();
            pipeStreamMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeStreamMock
                .Setup(pipe => pipe.Subscribe(It.IsAny<IObserver<byte[]>>()))
                .Callback<IObserver<byte[]>>(observer => pipeObserver = observer);
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);
            await target.InitAsync();

            // Act
            pipeObserver.OnNext(serializedMessage);
            pipeObserver.OnCompleted();

            // Assert
            handlerMock.Verify(handler => handler.OnRequestMessage(message.Payload), Times.Once());
        }

        [Fact]
        public async Task Dispose_DisposesObjects()
        {
            // Arrange
            var pipeStreamMock = new Mock<IPipeStream>();
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeStreamMock.Object, handlerMock.Object);
            await target.InitAsync();

            // Act
            target.Dispose();

            // Assert
            pipeStreamMock.Verify(pipe => pipe.Dispose(), Times.Once());
            handlerMock.Verify(handler => handler.Dispose(), Times.Once());
        }

        private static Messenger CreateMessenger(IPipeStream pipeStream, IMessageHandler handler = null, bool enableReconnect = false)
        {
            return new Messenger(
                () => pipeStream,
                handler ?? new Mock<IMessageHandler>().Object,
                enableReconnect);
        }
    }
}
