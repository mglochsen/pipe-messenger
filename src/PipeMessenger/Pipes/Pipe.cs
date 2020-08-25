using System;
using System.Collections.Generic;
using System.IO.Pipes;
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

        private bool _wasConnected;

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
            StartPipeObservation(_pipeStream, dataReceivedAction, disconnectedAction, _readCancellationTokenSource.Token);
        }

        public void Reconnect(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction)
        {
            CreateAndConnectPipeAsync(CancellationToken.None).Await(ex => { }, connectedAction, false);
            StartPipeObservation(dataReceivedAction, disconnectedAction);
        }

        public async Task WriteAsync(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            await _pipeStream.WriteAsync(data, 0, data.Length, _writeCancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _readCancellationTokenSource.Dispose();
            _writeCancellationTokenSource.Dispose();
            _pipeStream?.Dispose();
        }

        private async Task CreateAndConnectPipeAsync(CancellationToken cancellationToken)
        {
            _pipeStream?.Dispose();
            _pipeStream = _pipeStreamCreator();
            await _pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (_pipeStream.ReadMode != PipeTransmissionMode.Message)
            {
                _pipeStream.ReadMode = PipeTransmissionMode.Message;
            }

            _wasConnected = true;
        }

        private void StartPipeObservation(IPipeStream pipeStream, Action<byte[]> dataReceivedAction, Action disconnectedAction, CancellationToken cancellationToken)
        {
            if (pipeStream == null) throw new ArgumentNullException(nameof(pipeStream));
            if (dataReceivedAction == null) throw new ArgumentNullException(nameof(dataReceivedAction));

            Task.Run(
                async () =>
                {
                    while (IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var message = new List<byte>();
                        var messageBuffer = new byte[5];

                        do
                        {
                            var readBytes = await pipeStream.ReadAsync(messageBuffer, 0, messageBuffer.Length).ConfigureAwait(false);
                            if (readBytes != 0)
                            {
                                message.AddRange(messageBuffer.Take(readBytes));
                            }
                        } while (!pipeStream.IsMessageComplete);

                        if (message.Any())
                        {
                            dataReceivedAction(message.ToArray());
                        }
                        else if (_wasConnected)
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
            _wasConnected = false;
        }
    }
}
