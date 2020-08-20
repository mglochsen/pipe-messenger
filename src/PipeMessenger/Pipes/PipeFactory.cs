namespace PipeMessenger.Pipes
{
    internal static class PipeFactory
    {
        internal static IPipe CreatePipeServer(string pipeName)
        {
            return new PipeServer(pipeName);
        }

        internal static IPipe CreatePipeClient(string pipeName)
        {
            return new PipeClient(pipeName);
        }
    }
}
