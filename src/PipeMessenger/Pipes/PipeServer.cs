using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal class PipeServer : PipeBase
    {
        public PipeServer(string pipeName)
            : base(() => CreatePipeStream(pipeName))
        {
        }

        protected override Task ConnectPipeAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            var serverPipeStream = (NamedPipeServerStream)pipeStream;
            return serverPipeStream.WaitForConnectionAsync(cancellationToken);
        }

        private static PipeStream CreatePipeStream(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
    }
}
