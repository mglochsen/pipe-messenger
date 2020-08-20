using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal class PipeServer : PipeBase
    {
        public PipeServer(string pipeName)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.InOut))
        {
        }

        protected override Task ConnectPipeAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            var serverPipeStream = (NamedPipeServerStream)pipeStream;
            return serverPipeStream.WaitForConnectionAsync(cancellationToken);
        }
    }
}
