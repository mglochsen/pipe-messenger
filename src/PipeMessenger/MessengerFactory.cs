using System;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    /// <summary>
    /// Creates messenger instances.
    /// </summary>
    public static class MessengerFactory
    {
        public static IMessenger CreateServerMessenger(string messengerName, IMessageHandler handler, bool enableReconnect)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeServerStream($"{messengerName}-pipe"), handler, enableReconnect);
        }

        public static IMessenger CreateClientMessenger(string messengerName, IMessageHandler handler, bool enableReconnect)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeClientStream($"{messengerName}-pipe"), handler, enableReconnect);
        }
    }
}
