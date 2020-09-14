using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            string messengerName = GetMessengerName();
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
            string messengerName = GetMessengerName();
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
            string messengerName = GetMessengerName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());
            var expectedResponse = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler();
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler { ResponseToReturn = expectedResponse };
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);
            
            var initTasks = new[] { serverMessenger.InitAsync(), clientMessenger.InitAsync() };

            // Act
            await Task.WhenAll(initTasks);
            var requestId = await serverMessenger.SendRequestAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));

            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            requestId.Should().NotBeNull();
            serverMessageHandler.ReceivedResponse.Item1.Should().Be(requestId.GetValueOrDefault());
            serverMessageHandler.ReceivedResponse.Item2.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SendRequest_ClientToServer()
        {
            // Arrange
            string messengerName = GetMessengerName();
            var message = Encoding.UTF8.GetBytes(Path.GetRandomFileName());
            var expectedResponse = Encoding.UTF8.GetBytes(Path.GetRandomFileName());

            var serverMessageHandler = new TestMessageHandler { ResponseToReturn = expectedResponse };
            var serverMessenger = MessengerFactory.CreateServerMessenger(messengerName, serverMessageHandler, false);
            var clientMessageHandler = new TestMessageHandler();
            var clientMessenger = MessengerFactory.CreateClientMessenger(messengerName, clientMessageHandler, false);

            var initTasks = new[] { serverMessenger.InitAsync(), clientMessenger.InitAsync() };

            // Act
            await Task.WhenAll(initTasks);
            var requestId = await clientMessenger.SendRequestAsync(message);

            await Task.Delay(TimeSpan.FromSeconds(1));

            serverMessenger.Dispose();
            clientMessenger.Dispose();

            // Assert
            requestId.Should().NotBeNull();
            clientMessageHandler.ReceivedResponse.Item1.Should().Be(requestId.GetValueOrDefault());
            clientMessageHandler.ReceivedResponse.Item2.Should().BeEquivalentTo(expectedResponse);
        }

        private static string GetMessengerName([CallerMemberName] string caller = null)
        {
            return $"Messenger_{caller}";
        }

        private class TestMessageHandler : IMessageHandler
        {
            private readonly IList<byte[]> _receivedMessages = new List<byte[]>();

            public IEnumerable<byte[]> ReceivedMessages => _receivedMessages;

            public byte[] ResponseToReturn { get; set; }

            public Tuple<Guid, byte[]> ReceivedResponse { get; set; }

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
                return ResponseToReturn;
            }

            public void OnResponseMessage(Guid id, byte[] payloadBytes)
            {
                ReceivedResponse = new Tuple<Guid, byte[]>(id, payloadBytes);
            }

            public void Dispose()
            {
            }
        }
    }
}
