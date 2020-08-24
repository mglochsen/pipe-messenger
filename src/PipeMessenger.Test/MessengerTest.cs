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
        public void Init_InitializesPipe()
        {
            // Arrange
            var pipeMock = new Mock<IPipe>();
            var cancellationToken = new CancellationToken();
            var target = CreateMessenger(pipeMock.Object);

            // Act
            target.Init(cancellationToken);

            // Assert
            pipeMock.Verify(
                pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), cancellationToken),
                Times.Once());
        }

        [Fact]
        public void IsConnectedGetsValueFromPipe()
        {
            // Arrange
            var pipeMock = new Mock<IPipe>();
            pipeMock.SetupGet(pipe => pipe.IsConnected).Returns(true);
            var target = CreateMessenger(pipeMock.Object);

            // Act
            var isConnected = target.IsConnected;

            // Assert
            isConnected.Should().BeTrue();
            pipeMock.VerifyGet(
                pipe => pipe.IsConnected,
                Times.Once());
        }

        [Fact]
        public void SendWithoutResponseAsync_ThrowsException_WhenNotConnected()
        {
            // Arrange
            var pipeMock = new Mock<IPipe>();
            pipeMock.SetupGet(pipe => pipe.IsConnected).Returns(false);
            var payload = new byte[] { 1, 2, 3 };
            var target = CreateMessenger(pipeMock.Object);

            // Act and assert
            new Func<Task>(() => target.SendWithoutResponseAsync(payload)).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task SendWithoutResponseAsync_WritesMessage()
        {
            // Arrange
            var writtenBytes = new byte[0];
            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeMock
                .Setup(pipe => pipe.WriteAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(bytes => writtenBytes = bytes);
            var payload = new byte[] { 1, 2, 3 };
            var target = CreateMessenger(pipeMock.Object);

            // Act
            await target.SendWithoutResponseAsync(payload);

            // Assert
            pipeMock.Verify(
                pipe => pipe.WriteAsync(It.IsAny<byte[]>()),
                Times.Once());
            writtenBytes.Should().HaveCount(17 + payload.Length);
            writtenBytes[16].Should().Be((byte)MessageType.FireAndForget);
            writtenBytes.Skip(17).Should().BeEquivalentTo(payload);
        }

        [Fact]
        public async Task SendRequestAsync_GetsExpectedResponse()
        {
            // Arrange
            var writtenBytes = new byte[0];
            Action<byte[]> dataReceivedAction = null;

            var requestPayload = new byte[] { 1, 2, 3 };
            var responsePayload = new byte[] { 7, 8, 9 };

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            pipeMock
                .Setup(pipe => pipe.WriteAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(bytes => writtenBytes = bytes);

            var target = CreateMessenger(pipeMock.Object);
            target.Init(CancellationToken.None);

            // Act
            var sendRequestTask = target.SendRequestAsync(requestPayload);
            var messageId = MessageSerializer.DeserializeMessage(writtenBytes).Id;
            var responseMessage = new Message(messageId, MessageType.Response, responsePayload);
            dataReceivedAction(MessageSerializer.SerializeMessage(responseMessage));
            var requestResult = await sendRequestTask;

            // Assert
            requestResult.Should().BeEquivalentTo(responsePayload);
        }

        [Fact]
        public void MessengerInvokesConnectedAtHandler()
        {
            // Arrange
            Action connectedAction = null;

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => connectedAction = connected);

            var handlerMock = new Mock<IMessageHandler>();
            
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            connectedAction();

            // Assert
            handlerMock.Verify(handler => handler.OnConnected(), Times.Once());
        }

        [Fact]
        public void MessengerInvokesDisconnectedAtHandler()
        {
            // Arrange
            Action disconnectedAction = null;

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => disconnectedAction = disconnected);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            disconnectedAction();

            // Assert
            handlerMock.Verify(handler => handler.OnDisconnected(), Times.Once());
        }

        [Fact]
        public void MessengerDoesNotReconnect_WhenDisabled()
        {
            // Arrange
            Action disconnectedAction = null;

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => disconnectedAction = disconnected);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            disconnectedAction();

            // Assert
            pipeMock.Verify(pipe => pipe.Reconnect(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>()), Times.Never());
        }

        [Fact]
        public void MessengerReconnects_WhenEnabled()
        {
            // Arrange
            Action disconnectedAction = null;

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => disconnectedAction = disconnected);

            var handlerMock = new Mock<IMessageHandler>();

            var target = CreateMessenger(pipeMock.Object, handlerMock.Object, true);
            target.Init(CancellationToken.None);

            // Act
            disconnectedAction();

            // Assert
            pipeMock.Verify(pipe => pipe.Reconnect(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>()), Times.Once());
        }

        [Fact] public void MessengerHandlesFireAndForgetMessages()
        {
            // Arrange
            Action<byte[]> dataReceivedAction = null;
            var message = new Message(Guid.NewGuid(), MessageType.FireAndForget, new byte[] { 1, 2, 3 });
            var serializedMessage = MessageSerializer.SerializeMessage(message);

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            dataReceivedAction(serializedMessage);

            // Assert
            handlerMock.Verify(handler => handler.OnMessageWithoutResponse(message.Payload), Times.Once());
        }

        [Fact] public void MessengerHandlesRequestMessages()
        {
            // Arrange
            Action<byte[]> dataReceivedAction = null;
            var message = new Message(Guid.NewGuid(), MessageType.Request, new byte[] { 1, 2, 3 });
            var serializedMessage = MessageSerializer.SerializeMessage(message);

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            dataReceivedAction(serializedMessage);

            // Assert
            handlerMock.Verify(handler => handler.OnRequestMessage(message.Payload), Times.Once());
        }

        [Fact] public void Dispose_DisposesObjects()
        {
            // Arrange
            var pipeMock = new Mock<IPipe>();
            var handlerMock = new Mock<IMessageHandler>();
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object);

            // Act
            target.Dispose();

            // Assert
            pipeMock.Verify(pipe => pipe.Dispose(), Times.Once());
            handlerMock.Verify(handler => handler.Dispose(), Times.Once());
        }

        private static Messenger CreateMessenger(IPipe pipe, IMessageHandler handler = null, bool enableReconnect = false)
        {
            return new Messenger(
                () => pipe,
                handler ?? new Mock<IMessageHandler>().Object,
                enableReconnect);
        }
    }
}
