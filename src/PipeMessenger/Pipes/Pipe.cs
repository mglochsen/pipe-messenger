using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal class Pipe : IPipe
    {
        private readonly Func<IPipeStream> _pipeStreamCreator;
        private readonly CancellationTokenSource _readCancellationTokenSource;
        private readonly CancellationTokenSource _writeCancellationTokenSource;

        private IPipeStream _pipeStream;

        private bool _wasConnected;

        public Pipe(Func<IPipeStream> pipeStreamCreator)
        {
            _pipeStreamCreator = pipeStreamCreator ?? throw new ArgumentNullException(nameof(pipeStreamCreator));
            _readCancellationTokenSource = new CancellationTokenSource();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected => _pipeStream?.IsConnected ?? false;

        public async void Init(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction, CancellationToken cancellationToken)
        {
            _pipeStream?.Dispose();
            _pipeStream = _pipeStreamCreator();
            await _pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
            connectedAction?.Invoke();
            _wasConnected = true;
            StartPipeObservation(_pipeStream, dataReceivedAction, disconnectedAction, _readCancellationTokenSource.Token);
        }

        public async void Reconnect(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction)
        {
            _pipeStream?.Dispose();
            _pipeStream = _pipeStreamCreator();
            await _pipeStream.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
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
            _pipeStream?.Dispose();
        }

        private async void StartPipeObservation(IPipeStream pipeStream, Action<byte[]> dataReceivedAction, Action disconnectedAction, CancellationToken cancellationToken)
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
                        HandleDisconnected(disconnectedAction);
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
                            HandleDisconnected(disconnectedAction);
                        }
                    }
                    else
                    {
                        dataReceivedAction(data);
                    }
                }
            }
        }

        private void HandleDisconnected(Action disconnectedAction)
        {
            _pipeStream.Dispose();
            disconnectedAction?.Invoke();
            _wasConnected = false;
        }
    }
}
