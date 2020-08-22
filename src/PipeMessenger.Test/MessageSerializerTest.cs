using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace PipeMessenger.Test
{
    public class MessageSerializerTest
    {
        [Fact]
        public void SerializeMessage_ReturnsExpectedBytes()
        {
            // Arrange
            var message = new Message(Guid.NewGuid(), MessageType.Request, new byte[] { 1, 2, 3 });

            // Act
            var serializesMessage = MessageSerializer.SerializeMessage(message);

            // Assert
            serializesMessage.Should().HaveCount(17 + message.Payload.Length);
            serializesMessage.Take(16).Should().BeEquivalentTo(message.Id.ToByteArray());
            serializesMessage.Skip(16).First().Should().Be((byte) message.Type);
            serializesMessage.Skip(17).Should().BeEquivalentTo(message.Payload);
        }

        [Fact]
        public void DeserializeMessage_ReturnsExpectedMessage()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var messageType = MessageType.Response;
            var messagePayload = new byte[] { 1, 2, 3 };
            var serializedMessage = new List<byte>();
            serializedMessage.AddRange(messageId.ToByteArray());
            serializedMessage.Add((byte)messageType);
            serializedMessage.AddRange(messagePayload);

            // Act
            var message = MessageSerializer.DeserializeMessage(serializedMessage.ToArray());

            // Assert
            message.Id.Should().Be(messageId);
            message.Type.Should().Be(messageType);
            message.Payload.Should().BeEquivalentTo(messagePayload);
        }

        [Fact]
        public void DeserializeMessage_ThrowsExceptionWhenInsufficientBytes()
        {
            // Arrange
            var serializedMessage = new byte[10];

            // Act and assert
            new Action(() => MessageSerializer.DeserializeMessage(serializedMessage))
                .Should().Throw<ArgumentException>();
        }
    }
}
