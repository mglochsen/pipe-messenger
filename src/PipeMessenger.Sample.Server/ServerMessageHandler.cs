using System;
using System.Text;
using System.Threading.Tasks;
using PipeMessenger.Sample.Common;

namespace PipeMessenger.Sample.Server
{
    public class ServerMessageHandler : MessageHandlerBase
    {
        private readonly Func<byte[], Task<byte[]>> _sendRequestAction;

        public ServerMessageHandler(Func<byte[], Task<byte[]>> sendRequestAction)
        {
            _sendRequestAction = sendRequestAction ?? throw new ArgumentNullException(nameof(sendRequestAction));
        }

        public override async void OnConnected()
        {
            base.OnConnected();

            var content = "Hello from server";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            Console.WriteLine("Sending request");
            var responseBytes = await _sendRequestAction(contentBytes).ConfigureAwait(false);
            var response = Encoding.UTF8.GetString(responseBytes);
            Console.WriteLine($"Returned response: {response}");
        }
    }
}
