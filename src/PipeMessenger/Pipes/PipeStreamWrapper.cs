using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive.Linq;
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

        public async Task<bool> WriteAsync(byte[] data, CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;
            var dataLength = BitConverter.GetBytes(data.Length);
            var dataWithLength = dataLength.Concat(data).ToArray();

            try
            {
                await _pipeStream.WriteAsync(dataWithLength, 0, dataWithLength.Length, token).ConfigureAwait(false);
                await _pipeStream.FlushAsync(token).ConfigureAwait(false);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public IDisposable Subscribe(IObserver<byte[]> observer)
        {
            return Observable.FromAsync(ReadAsync).Repeat().TakeUntil(data => data == null).Subscribe(observer);
        }

        public void Dispose()
        {
            _pipeStream.Dispose();
        }

        private async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var dataLengthBuffer = new byte[sizeof(int)];

                var readBytes = await _pipeStream.ReadAsync(dataLengthBuffer, 0, dataLengthBuffer.Length, cancellationToken).ConfigureAwait(false);
                if (readBytes == 0)
                {
                    return null;
                }

                var dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);
                var data = new byte[dataLength];
                readBytes = await _pipeStream.ReadAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
                if (readBytes == 0)
                {
                    return null;
                }

                return data;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }
}
