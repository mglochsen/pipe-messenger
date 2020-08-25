using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public sealed class Messenger : IMessenger
    {
        private readonly IPipe _pipe;
        private readonly IMessageHandler _handler;
        private readonly bool _enableReconnect;

        private readonly IDictionary<Guid, TaskCompletionSource<byte[]>> _pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>>();

        internal Messenger(Func<IPipe> pipeCreator, IMessageHandler handler, bool enableReconnect)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _enableReconnect = enableReconnect;
            _pipe = pipeCreator();
        }

        public bool IsConnected => _pipe.IsConnected;

        public void Init(CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;
            _pipe.Init(OnConnected, token);
            _pipe.StartPipeObservation(OnDataReceived, OnDisconnected);
        }

        public async Task SendWithoutResponseAsync(byte[] payload)
        {
            var message = new Message(Guid.NewGuid(), MessageType.FireAndForget, payload);
            var data = MessageSerializer.SerializeMessage(message);
            await WriteAsync(data).ConfigureAwait(false);
        }

        public async Task<byte[]> SendRequestAsync(byte[] payload)
        {
            var message = new Message(Guid.NewGuid(), MessageType.Request, payload);
            var data = MessageSerializer.SerializeMessage(message);

            await WriteAsync(data).ConfigureAwait(false);

            var tsc = new TaskCompletionSource<byte[]>();
            _pendingRequests.Add(message.Id, tsc);

            return await tsc.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            _handler.Dispose();
            _pendingRequests.Clear();
            _pipe?.Dispose();
        }

        private async Task WriteAsync(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Messenger is not connected");
            }

            await _pipe.WriteAsync(data).ConfigureAwait(false);
        }

        private void OnConnected()
        {
            _handler.OnConnected();
        }

        private void OnDisconnected()
        {
            _handler.OnDisconnected();

            if (_enableReconnect)
            {
                _pipe.Reconnect(OnConnected, OnDisconnected, OnDataReceived);
            }
        }

        private async void OnDataReceived(byte[] data)
        {
            var message = MessageSerializer.DeserializeMessage(data);
            switch (message.Type)
            {
                case MessageType.FireAndForget:
                    _handler.OnMessageWithoutResponse(message.Payload);
                    break;
                case MessageType.Request:
                    var response = _handler.OnRequestMessage(message.Payload);
                    var responseMessage = new Message(message.Id, MessageType.Response, response);
                    var responseData = MessageSerializer.SerializeMessage(responseMessage);
                    await WriteAsync(responseData).ConfigureAwait(false);
                    break;
                case MessageType.Response:
                    if (_pendingRequests.TryGetValue(message.Id, out var pendingRequest))
                    {
                        _pendingRequests.Remove(message.Id);
                        pendingRequest.SetResult(message.Payload);
                    }

                    break;
            }
        }
    }
}
