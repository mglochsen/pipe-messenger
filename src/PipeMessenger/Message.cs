using System;

namespace PipeMessenger
{
    internal class Message
    {
        public Message(Guid id, MessageType type, byte[] payload)
        {
            Id = id;
            Type = type;
            Payload = payload;
        }

        public Guid Id { get; }

        public MessageType Type { get; }

        public byte[] Payload { get; }
    }
}
