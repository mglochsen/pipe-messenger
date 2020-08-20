namespace PipeMessenger
{
    public sealed class Message
    {
        public string Id { get; set; }

        public MessageType Type { get; set; }

        public byte[] Payload { get; set; }
    }
}
