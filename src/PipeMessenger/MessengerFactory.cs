using System;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public static class MessengerFactory
    {
        public static IMessenger CreateServerMessenger(string messengerName, IMessageHandler handler, bool enableReconnect)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeServer($"{messengerName}-name"), handler, enableReconnect);
        }

        public static IMessenger CreateClientMessenger(string messengerName, IMessageHandler handler, bool enableReconnect)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeClient($"{messengerName}-name"), handler, enableReconnect);
        }
    }
}
