namespace PipeMessenger.Contracts
{
    public interface IMessageHandler
    {
        void OnConnected();

        void OnDisconnected();

        void OnMessageWithoutResponse(byte[] payloadBytes);

        byte[] OnRequestMessage(byte[] payloadBytes);
    }
}
