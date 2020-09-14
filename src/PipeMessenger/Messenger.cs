using System;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public sealed class Messenger : IMessenger
    {
        private readonly Func<IPipeStream> _pipeStreamCreator;
        private readonly IMessageHandler _handler;
        private readonly bool _enableReconnect;

        private IPipeStream _pipeStream;

        internal Messenger(Func<IPipeStream> pipeStreamCreator, IMessageHandler handler, bool enableReconnect)
        {
            _pipeStreamCreator = pipeStreamCreator ?? throw new ArgumentNullException(nameof(pipeStreamCreator));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _enableReconnect = enableReconnect;
        }

        public bool IsConnected => _pipeStream?.IsConnected ?? false;

        public async Task InitAsync(CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;
            _pipeStream?.Dispose();
            _pipeStream = _pipeStreamCreator();
            await _pipeStream.ConnectAsync(token).ConfigureAwait(false);
            OnConnected();
            _pipeStream.Subscribe(
                OnDataReceived,
                ex => { },
                () => { },
                token);
        }

        public async Task<bool> SendAsync(byte[] payload)
        {
            var message = new Message(Guid.NewGuid(), MessageType.FireAndForget, payload);
            var data = MessageSerializer.SerializeMessage(message);
            return await WriteAsync(data).ConfigureAwait(false);
        }

        public async Task<Guid?> SendRequestAsync(byte[] payload)
        {
            var message = new Message(Guid.NewGuid(), MessageType.Request, payload);
            var data = MessageSerializer.SerializeMessage(message);

            var dataWritten = await WriteAsync(data).ConfigureAwait(false);
            if (!dataWritten)
            {
                return null;
            }

            return message.Id;
        }

        public void Dispose()
        {
            _handler.Dispose();
            _pipeStream?.Dispose();
        }

        private async Task<bool> WriteAsync(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Messenger is not connected");
            }

            return await _pipeStream.WriteAsync(data).ConfigureAwait(false);
        }

        private void OnConnected()
        {
            _handler.OnConnected();
        }

        private async void OnDisconnected()
        {
            _handler.OnDisconnected();

            if (_enableReconnect)
            {
                await InitAsync().ConfigureAwait(false);
            }
        }

        private async void OnDataReceived(byte[] data)
        {
            if (data == null)
            {
                OnDisconnected();
                return;
            }

            var message = MessageSerializer.DeserializeMessage(data);
            switch (message.Type)
            {
                case MessageType.FireAndForget:
                    _handler.OnMessage(message.Payload);
                    break;
                case MessageType.Request:
                    var response = _handler.OnRequestMessage(message.Payload);
                    var responseMessage = new Message(message.Id, MessageType.Response, response);
                    var responseData = MessageSerializer.SerializeMessage(responseMessage);
                    await WriteAsync(responseData).ConfigureAwait(false);
                    break;
                case MessageType.Response:
                    _handler.OnResponseMessage(message.Id, message.Payload);
                    break;
            }
        }
    }
}
