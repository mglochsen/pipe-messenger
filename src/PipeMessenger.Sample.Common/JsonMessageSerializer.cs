using System.Text;
using Newtonsoft.Json;
using PipeMessenger.Contracts;

namespace PipeMessenger.Sample.Common
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(Message message)
        {
            var serializedMessage = JsonConvert.SerializeObject(message);
            var serializedMessageBytes = Encoding.UTF8.GetBytes(serializedMessage);
            return serializedMessageBytes;
        }

        public Message Deserialize(byte[] bytes)
        {
            var serializedMessage = Encoding.UTF8.GetString(bytes);
            var message = JsonConvert.DeserializeObject<Message>(serializedMessage);
            return message;
        }
    }
}
