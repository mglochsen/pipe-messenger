namespace PipeMessenger.Contracts
{
    public interface IMessageSerializer
    {
        byte[] Serialize(Message message);

        Message Deserialize(byte[] bytes);
    }
}
