using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Extensions;

namespace PipeMessenger.Pipes
{
    internal class Pipe : IPipe
    {
        private readonly Func<IPipeStream> _pipeStreamCreator;
        private readonly CancellationTokenSource _readCancellationTokenSource;
        private readonly CancellationTokenSource _writeCancellationTokenSource;

        private IPipeStream _pipeStream;

        public Pipe(Func<IPipeStream> pipeStreamCreator)
        {
            _pipeStreamCreator = pipeStreamCreator ?? throw new ArgumentNullException(nameof(pipeStreamCreator));
            _readCancellationTokenSource = new CancellationTokenSource();
            _writeCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected => _pipeStream?.IsConnected ?? false;

        public void Init(Action connectedAction, CancellationToken cancellationToken)
        {
            CreateAndConnectPipeAsync(cancellationToken).Await(ex => {}, connectedAction, false);
        }

        public void StartPipeObservation(Action<byte[]> dataReceivedAction, Action disconnectedAction)
        {
            StartPipeObservationTask(dataReceivedAction, disconnectedAction);
        }

        public void Reconnect(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction)
        {
            CreateAndConnectPipeAsync(CancellationToken.None).Await(ex => { }, connectedAction, false);
            StartPipeObservation(dataReceivedAction, disconnectedAction);
        }

        public async Task WriteAsync(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            await WriteDataAsync(_pipeStream, data, _writeCancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _readCancellationTokenSource.Dispose();
            _writeCancellationTokenSource.Dispose();
            _pipeStream?.Dispose();
        }

        private static async Task<byte[]> ReadDataAsync(IPipeStream pipeStream, CancellationToken cancellationToken)
        {
            var dataLengthBuffer = new byte[sizeof(int)];

            var readBytes = await pipeStream.ReadAsync(dataLengthBuffer, 0, dataLengthBuffer.Length, cancellationToken).ConfigureAwait(false);
            if (readBytes == 0)
            {
                return null;
            }

            var dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);
            var data = new byte[dataLength];
            readBytes = await pipeStream.ReadAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            if (readBytes == 0)
            {
                return null;
            }

            return data;
        }

        private static async Task WriteDataAsync(IPipeStream pipeStream, byte[] data, CancellationToken cancellationToken)
        {
            var dataLength = BitConverter.GetBytes(data.Length);
            var dataWithLength = dataLength.Concat(data).ToArray();

            await pipeStream.WriteAsync(dataWithLength, 0, dataWithLength.Length, cancellationToken).ConfigureAwait(false);
        }

        private async Task CreateAndConnectPipeAsync(CancellationToken cancellationToken)
        {
            _pipeStream?.Dispose();
            _pipeStream = _pipeStreamCreator();
            await _pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        private void StartPipeObservationTask(Action<byte[]> dataReceivedAction, Action disconnectedAction)
        {
            if (dataReceivedAction == null) throw new ArgumentNullException(nameof(dataReceivedAction));

            var cancellationToken = _readCancellationTokenSource.Token;

            Task.Run(
                async () =>
                {
                    while (IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var data = await ReadDataAsync(_pipeStream, cancellationToken).ConfigureAwait(false);

                        if (data != null)
                        {
                            dataReceivedAction(data);
                        }
                        else
                        {
                            HandleDisconnected(disconnectedAction);
                        }
                    }
                },
                cancellationToken);
        }

        private void HandleDisconnected(Action disconnectedAction)
        {
            _pipeStream.Dispose();
            disconnectedAction?.Invoke();
        }
    }
}
