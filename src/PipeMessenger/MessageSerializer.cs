using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeMessenger
{
    internal static class MessageSerializer
    {
        public static byte[] SerializeMessage(Message message)
        {
            var bytes = new List<byte>();
            bytes.AddRange(message.Id.ToByteArray());
            bytes.Add((byte)message.Type);
            bytes.AddRange(message.Payload);
            return bytes.ToArray();
        }

        public static Message DeserializeMessage(byte[] data)
        {
            if (data.Length < 17)
            {
                throw new ArgumentException("Invalid data", nameof(data));
            }

            var idBytes = data.Take(16).ToArray();
            var typeByte = data[16];
            var payload = data.Skip(17).ToArray();

            var id = new Guid(idBytes);
            var type = (MessageType)typeByte;

            return new Message(id, type, payload);
        }
    }
}
