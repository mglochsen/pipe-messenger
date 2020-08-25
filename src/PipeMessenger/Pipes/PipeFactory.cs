using System.IO.Pipes;

namespace PipeMessenger.Pipes
{
    internal static class PipeFactory
    {
        internal static IPipe CreatePipeServer(string pipeName)
        {
            return new Pipe(() => new PipeStreamWrapper(CreatePipeServerStream(pipeName)));
        }

        internal static IPipe CreatePipeClient(string pipeName)
        {
            return new Pipe(() => new PipeStreamWrapper(CreatePipeClientStream(pipeName)));
        }

        private static PipeStream CreatePipeServerStream(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        }

        private static PipeStream CreatePipeClientStream(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }
    }
}
