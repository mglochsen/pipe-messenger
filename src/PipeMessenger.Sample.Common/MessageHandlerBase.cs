using System;
using System.Text;
using PipeMessenger.Contracts;

namespace PipeMessenger.Sample.Common
{
    public abstract class MessageHandlerBase : IMessageHandler
    {
        protected bool IsConnected { get; private set; }

        public virtual void OnConnected()
        {
            Console.WriteLine("Messenger is connected");
            IsConnected = true;
        }

        public virtual void OnDisconnected()
        {
            Console.WriteLine("Messenger is disconnected");
            IsConnected = false;
        }

        public virtual void OnMessageWithoutResponse(byte[] payloadBytes)
        {
            var payload = Encoding.UTF8.GetString(payloadBytes);
            Console.WriteLine($"Received message: {payload}");
        }

        public virtual byte[] OnRequestMessage(byte[] payloadBytes)
        {
            var payload = Encoding.UTF8.GetString(payloadBytes);
            Console.WriteLine($"Received request: {payload}");

            var response = $"Response to: {payload}";
            var responseBytes = Encoding.UTF8.GetBytes(response);

            return responseBytes;
        }
    }
}
