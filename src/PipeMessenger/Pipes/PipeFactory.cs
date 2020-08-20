namespace PipeMessenger.Pipes
{
    internal static class PipeFactory
    {
        internal static PipeBase CreatePipeServer(string pipeName)
        {
            return new PipeServer(pipeName);
        }

        internal static PipeBase CreatePipeClient(string pipeName)
        {
            return new PipeClient(pipeName);
        }
    }
}
