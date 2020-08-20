using System;
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
        public async Task SendWithoutResponseAsync_SerializesAndWritesMessage()
        {
            // Arrange
            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            var serializedMessage = new byte[] { 0, 8, 15 };
            var messageSerializerMock = new Mock<IMessageSerializer>();
            messageSerializerMock
                .Setup(serializer => serializer.Serialize(It.IsAny<Message>()))
                .Returns(serializedMessage);
            var payload = new byte[] { 1, 2, 3 };
            var target = CreateMessenger(pipeMock.Object, messageSerializer: messageSerializerMock.Object);

            // Act
            await target.SendWithoutResponseAsync(payload);

            // Assert
            messageSerializerMock.Verify(
                serializer => serializer.Serialize(It.Is<Message>(message => message.Type == MessageType.FireAndForget)),
                Times.Once());
            pipeMock.Verify(
                pipe => pipe.WriteAsync(serializedMessage),
                Times.Once());
        }

        [Fact]
        public async Task SendRequestAsync_GetsExpectedResponse()
        {
            // Arrange
            Action<byte[]> dataReceivedAction = null;

            string messageId = null;
            var requestPayload = new byte[] { 1, 2, 3 };
            var response = new byte[] { 4, 5, 6 };
            var responsePayload = new byte[] {7, 8, 9 };

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            
            var messageSerializerMock = new Mock<IMessageSerializer>();
            messageSerializerMock
                .Setup(serializer => serializer.Serialize(It.Is<Message>(message => message.Type == MessageType.Request)))
                .Callback<Message>(message => messageId = message.Id);
            messageSerializerMock
                .Setup(serializer => serializer.Deserialize(response))
                .Returns(() => new Message { Id = messageId, Type = MessageType.Response, Payload = responsePayload });
            
            var target = CreateMessenger(pipeMock.Object, messageSerializer: messageSerializerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            var sendRequestTask = target.SendRequestAsync(requestPayload);
            dataReceivedAction(response);
            var requestResult = await sendRequestTask;

            // Assert
            requestResult.Should().BeSameAs(responsePayload);
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

        [Fact] public void MessengerHandlesFireAndForgetMessages()
        {
            // Arrange
            Action<byte[]> dataReceivedAction = null;
            var serializedMessage = new byte[] { 1, 2, 3 };
            var messagePayload = new byte[] { 4, 5, 6 };

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            var handlerMock = new Mock<IMessageHandler>();
            var messageSerializerMock = new Mock<IMessageSerializer>();
            messageSerializerMock
                .Setup(serializer => serializer.Deserialize(serializedMessage))
                .Returns(() => new Message { Type = MessageType.FireAndForget, Payload = messagePayload });
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object, messageSerializerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            dataReceivedAction(serializedMessage);

            // Assert
            handlerMock.Verify(handler => handler.OnMessageWithoutResponse(messagePayload), Times.Once());
        }

        [Fact] public void MessengerHandlesRequestMessages()
        {
            // Arrange
            Action<byte[]> dataReceivedAction = null;
            var serializedMessage = new byte[] { 1, 2, 3 };
            var messagePayload = new byte[] { 4, 5, 6 };

            var pipeMock = new Mock<IPipe>();
            pipeMock
                .SetupGet(pipe => pipe.IsConnected)
                .Returns(true);
            pipeMock
                .Setup(pipe => pipe.Init(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<CancellationToken>()))
                .Callback<Action, Action, Action<byte[]>, CancellationToken>((connected, disconnected, dataReceived, token) => dataReceivedAction = dataReceived);
            var handlerMock = new Mock<IMessageHandler>();
            var messageSerializerMock = new Mock<IMessageSerializer>();
            messageSerializerMock
                .Setup(serializer => serializer.Deserialize(serializedMessage))
                .Returns(() => new Message { Type = MessageType.Request, Payload = messagePayload });
            var target = CreateMessenger(pipeMock.Object, handlerMock.Object, messageSerializerMock.Object);
            target.Init(CancellationToken.None);

            // Act
            dataReceivedAction(serializedMessage);

            // Assert
            handlerMock.Verify(handler => handler.OnRequestMessage(messagePayload), Times.Once());
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

        private static Messenger CreateMessenger(
            IPipe pipe,
            IMessageHandler handler = null,
            IMessageSerializer messageSerializer = null)
        {
            return new Messenger(
                () => pipe ?? new Mock<IPipe>().Object,
                handler ?? new Mock<IMessageHandler>().Object,
                messageSerializer ?? new Mock<IMessageSerializer>().Object);
        }
    }
}
