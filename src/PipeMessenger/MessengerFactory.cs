using System;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public static class MessengerFactory
    {
        public static Messenger CreateServerMessenger(string messengerName, IMessageHandler handler, IMessageSerializer messageSerializer)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));
            if (messageSerializer == null) throw new ArgumentNullException(nameof(messageSerializer));

            return new Messenger(() => PipeFactory.CreatePipeServer($"{messengerName}-name"), handler, messageSerializer);
        }

        public static Messenger CreateClientMessenger(string messengerName, IMessageHandler handler, IMessageSerializer messageSerializer)
        {
            if (string.IsNullOrEmpty(messengerName)) throw new ArgumentException("Messenger name must not be empty", nameof(messengerName));
            if (messageSerializer == null) throw new ArgumentNullException(nameof(messageSerializer));

            return new Messenger(() => PipeFactory.CreatePipeClient($"{messengerName}-name"), handler, messageSerializer);
        }
    }
}
