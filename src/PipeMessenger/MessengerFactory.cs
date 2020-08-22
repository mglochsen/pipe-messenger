using System;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public static class MessengerFactory
    {
        public static Messenger CreateServerMessenger(string messengerName, IMessageHandler handler)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeServer($"{messengerName}-name"), handler);
        }

        public static Messenger CreateClientMessenger(string messengerName, IMessageHandler handler)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));

            return new Messenger(() => PipeFactory.CreatePipeClient($"{messengerName}-name"), handler);
        }
    }
}
