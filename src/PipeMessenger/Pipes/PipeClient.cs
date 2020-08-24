using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal class PipeClient : PipeBase
    {
        public PipeClient(string pipeName)
            : base(() => new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
        {
        }

        protected override Task ConnectPipeAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            var clientPipeStream = (NamedPipeClientStream)pipeStream;
            return clientPipeStream.ConnectAsync(cancellationToken);
        }
    }
}
