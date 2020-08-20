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
        private readonly PipeBase _pipe;

        private readonly IDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

        private IMessageHandler _handler;

        internal Messenger(Func<PipeBase> pipeCreator, IMessageSerializer messageSerializer)
        {
            _messageSerializer = messageSerializer;
            _pipe = pipeCreator();
        }

        public bool IsConnected => _pipe.IsConnected;

        public void Init(IMessageHandler handler, CancellationToken? cancellationToken = null)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            var token = cancellationToken ?? CancellationToken.None;
            _pipe.Init(
                () => _handler.OnConnected(),
                () => _handler.OnDisconnected(),
                DataReceived,
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
            _handler = null;
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

        private async void DataReceived(byte[] data)
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
                default:
                    throw new ArgumentOutOfRangeException();
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
