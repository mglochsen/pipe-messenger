using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using PipeMessenger.Contracts;
using Xunit;

namespace PipeMessenger.Test
{
    public class MessengerIntegrationTest
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task SendMessage_ServerToClient()
        {
            // Arrange
            string messengerName = Path.GetRandomFileName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler();
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler();
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);

            var initTasks = new[] {serverMessenger.InitAsync(), clientMessenger.InitAsync()};

            // Act
            await Task.WhenAll(initTasks);
            await serverMessenger.SendAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));
            
            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            clientMessageHandler.ReceivedMessages.Single().Should().BeEquivalentTo(message);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SendMessage_ClientToServer()
        {
            // Arrange
            string messengerName = Path.GetRandomFileName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler();
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler();
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);

            var initTasks = new[] { serverMessenger.InitAsync(), clientMessenger.InitAsync() };

            // Act
            await Task.WhenAll(initTasks);
            await clientMessenger.SendAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));

            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            serverMessageHandler.ReceivedMessages.Single().Should().BeEquivalentTo(message);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SendRequest_ServerToClient()
        {
            // Arrange
            string messengerName = Path.GetRandomFileName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());
            var expectedResponse = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler();
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler { ResponsePayload = expectedResponse };
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);
            
            var initTasks = new[] { serverMessenger.InitAsync(), clientMessenger.InitAsync() };

            // Act
            await Task.WhenAll(initTasks);
            var response = await serverMessenger.SendRequestAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));

            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SendRequest_ClientToServer()
        {
            // Arrange
            string messengerName = Path.GetRandomFileName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());
            var expectedResponse = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler { ResponsePayload = expectedResponse };
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler();
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);
            
            var initTasks = new[] { serverMessenger.InitAsync(), clientMessenger.InitAsync() };

            // Act
            await Task.WhenAll(initTasks);
            var response = await clientMessenger.SendRequestAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));

            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            response.Should().BeEquivalentTo(expectedResponse);
        }

        private class TestMessageHandler : IMessageHandler
        {
            private readonly IList<byte[]> _receivedMessages = new List<byte[]>();

            public IEnumerable<byte[]> ReceivedMessages => _receivedMessages;

            public byte[] ResponsePayload { get; set; }

            public void OnConnected()
            {
            }

            public void OnDisconnected()
            {
            }

            public void OnMessage(byte[] payloadBytes)
            {
                _receivedMessages.Add(payloadBytes);
            }

            public byte[] OnRequestMessage(byte[] payloadBytes)
            {
                return ResponsePayload;
            }

            public void Dispose()
            {
            }
        }
    }
}
