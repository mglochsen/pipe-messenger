using System.IO.Pipes;

namespace PipeMessenger.Pipes
{
    internal static class PipeFactory
    {
        internal static IPipeStream CreatePipeServerStream(string pipeName)
        {
            return new PipeStreamWrapper(CreateSystemPipeServerStream(pipeName));
        }

        internal static IPipeStream CreatePipeClientStream(string pipeName)
        {
            return new PipeStreamWrapper(CreateSystemPipeClientStream(pipeName));
        }

        private static PipeStream CreateSystemPipeServerStream(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        private static PipeStream CreateSystemPipeClientStream(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }
    }
}
