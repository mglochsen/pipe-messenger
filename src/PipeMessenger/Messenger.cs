using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Contracts;
using PipeMessenger.Pipes;

namespace PipeMessenger
{
    public class Messenger : IDisposable
    {
        private readonly IMessageSerializer _messageSerializer;
        private readonly IPipe _pipe;
        private readonly IMessageHandler _handler;

        private readonly IDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();


        internal Messenger(Func<IPipe> pipeCreator, IMessageHandler handler, IMessageSerializer messageSerializer)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
            _pipe = pipeCreator();
        }

        public bool IsConnected => _pipe.IsConnected;

        public void Init(CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;
            _pipe.Init(
                OnConnected,
                OnDisconnected,
                OnDataReceived,
                token);
        }

        public async Task SendWithoutResponseAsync(byte[] payload)
        {
            var id = Guid.NewGuid().ToString();
            var data = CreateAndSerializeMessage(id, MessageType.FireAndForget, payload);
            await WriteAsync(data).ConfigureAwait(false);
        }

        public async Task<byte[]> SendRequestAsync(byte[] payload)
        {
            var id = Guid.NewGuid().ToString();
            var data = CreateAndSerializeMessage(id, MessageType.Request, payload);

            await WriteAsync(data).ConfigureAwait(false);

            var tsc = new TaskCompletionSource<byte[]>();
            _pendingRequests.Add(id, tsc);

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
        }

        private async void OnDataReceived(byte[] data)
        {
            var message = _messageSerializer.Deserialize(data);
            switch (message.Type)
            {
                case MessageType.FireAndForget:
                    _handler.OnMessageWithoutResponse(message.Payload);
                    break;
                case MessageType.Request:
                    var response = _handler.OnRequestMessage(message.Payload);
                    var responseData = CreateAndSerializeMessage(message.Id, MessageType.Response, response);
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

        private byte[] CreateAndSerializeMessage(string id, MessageType messageType, byte[] payload)
        {
            var message = new Message
            {
                Id = id,
                Type = messageType,
                Payload = payload
            };

            return _messageSerializer.Serialize(message);
        }
    }
}
