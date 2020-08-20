using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Sample.Common;

namespace PipeMessenger.Sample.Client
{
    public class ClientMessageHandler : MessageHandlerBase
    {
        private readonly Func<byte[], Task> _sendMessageWithoutResponseAction;
        private readonly CancellationToken _cancellationToken;

        public ClientMessageHandler(Func<byte[], Task> sendMessageWithoutResponseAction, CancellationToken cancellationToken)
        {
            _sendMessageWithoutResponseAction = sendMessageWithoutResponseAction ?? throw new ArgumentNullException(nameof(sendMessageWithoutResponseAction));
            _cancellationToken = cancellationToken;
        }

        public override async void OnConnected()
        {
            base.OnConnected();

            while (IsConnected && !_cancellationToken.IsCancellationRequested)
            {
                var content = $"Hello at {DateTime.Now.TimeOfDay}";
                var contentBytes = Encoding.UTF8.GetBytes(content);

                Console.WriteLine("Sending message");
                await _sendMessageWithoutResponseAction(contentBytes).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationToken);
            }
        }
    }
}
