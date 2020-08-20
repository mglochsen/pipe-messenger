using System;
using System.Threading;
using PipeMessenger.Sample.Common;

namespace PipeMessenger.Sample.Client
{
    class Program
    {
        static Messenger _messenger;
        static CancellationToken _cancellationToken;

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = cancellationTokenSource.Token;

            Console.WriteLine("Creating client messenger");
            var messenger = MessengerFactory.CreateClientMessenger("SampleMessenger", new JsonMessageSerializer());
            var handler = new ClientMessageHandler(payloadBytes => messenger.SendWithoutResponseAsync(payloadBytes), _cancellationToken);
            _messenger = messenger;

            Console.WriteLine("Initializing client messenger");
            _messenger.Init(handler, _cancellationToken);

            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }
    }
}
