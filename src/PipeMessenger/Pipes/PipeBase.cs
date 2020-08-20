using System;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal abstract class PipeBase : IDisposable
    {
        private readonly PipeStream _pipeStream;
        private readonly CancellationTokenSource _readCancellationTokenSource;
        private readonly CancellationTokenSource _writeCancellationTokenSource;

        private bool _wasConnected = false;

        protected PipeBase(PipeStream pipeStream)
        {
            _pipeStream = pipeStream;
            _readCancellationTokenSource = new CancellationTokenSource();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected => _pipeStream.IsConnected;

        public async void Init(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction, CancellationToken cancellationToken)
        {
            await ConnectPipeAsync(_pipeStream, cancellationToken).ConfigureAwait(false);
            connectedAction?.Invoke();
            _wasConnected = true;
            StartPipeObservation(_pipeStream, dataReceivedAction, disconnectedAction, _readCancellationTokenSource.Token);
        }

        public Task WriteAsync(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var dataLength = BitConverter.GetBytes(data.Length);
            var bytes = dataLength.Concat(data).ToArray();

            return _pipeStream.WriteAsync(bytes, 0, bytes.Length, _writeCancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _readCancellationTokenSource.Dispose();
            _writeCancellationTokenSource.Dispose();
            _pipeStream.Dispose();
        }

        protected abstract Task ConnectPipeAsync(PipeStream pipeStream, CancellationToken cancellationToken);

        private async void StartPipeObservation(PipeStream pipeStream, Action<byte[]> dataReceivedAction, Action disconnectedAction, CancellationToken cancellationToken)
        {
            if (pipeStream == null) throw new ArgumentNullException(nameof(pipeStream));
            if (dataReceivedAction == null) throw new ArgumentNullException(nameof(dataReceivedAction));

            var bufferSize = sizeof(int);
            var buffer = new byte[bufferSize];

            while (IsConnected && !cancellationToken.IsCancellationRequested)
            {
                var readBytes = await pipeStream.ReadAsync(buffer, 0, bufferSize, cancellationToken).ConfigureAwait(false);
                if (readBytes == 0)
                {
                    if (_wasConnected)
                    {
                        disconnectedAction?.Invoke();
                        _wasConnected = false;
                    }
                }
                else
                {
                    var dataLength = BitConverter.ToInt32(buffer, 0);
                    var data = new byte[dataLength];

                    var readDataBytes = await pipeStream.ReadAsync(data, 0, dataLength, cancellationToken).ConfigureAwait(false);
                    if (readDataBytes == 0)
                    {
                        if (_wasConnected)
                        {
                            disconnectedAction?.Invoke();
                            _wasConnected = false;
                        }
                    }
                    else
                    {
                        dataReceivedAction(data);
                    }
                }
            }
        }
    }
}
