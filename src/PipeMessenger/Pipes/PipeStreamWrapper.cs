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

        public PipeTransmissionMode ReadMode
        {
            get => _pipeStream.ReadMode;
            set => _pipeStream.ReadMode = value;
        }

        public bool IsMessageComplete => _pipeStream.IsMessageComplete;

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            switch (_pipeStream)
            {
                case NamedPipeServerStream pipeStream:
                    await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case NamedPipeClientStream pipeStream:
                    await pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("Only supported by named pipe streams");
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null)
        {
            return await _pipeStream.ReadAsync(buffer, offset, count, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;
            await _pipeStream.WriteAsync(buffer, offset, count, token).ConfigureAwait(false);
            await _pipeStream.FlushAsync(token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _pipeStream.Dispose();
        }
    }
}
