using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal class PipeStreamWrapper : IPipeStream
    {
        private readonly PipeStream _pipeStream;

        public PipeStreamWrapper(PipeStream pipeStream)
        {
            _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
        }

        public bool IsConnected => _pipeStream.IsConnected;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            switch (_pipeStream)
            {
                case NamedPipeServerStream pipeStream:
                    return pipeStream.WaitForConnectionAsync(cancellationToken);
                case NamedPipeClientStream pipeStream:
                    return pipeStream.ConnectAsync(cancellationToken);
                default:
                    throw new InvalidOperationException("Only supported by named pipe streams");
            }
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null)
        {
            return _pipeStream.ReadAsync(buffer, offset, count, cancellationToken ?? CancellationToken.None);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null)
        {
            return _pipeStream.WriteAsync(buffer, offset, count, cancellationToken ?? CancellationToken.None);
        }

        public void Dispose()
        {
            _pipeStream.Dispose();
        }
    }
}
