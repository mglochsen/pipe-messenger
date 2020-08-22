using System;
using System.Threading;
using System.Threading.Tasks;

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
            var handler = new ClientMessageHandler(SendWithoutResponseAsync, _cancellationToken);
            _messenger = MessengerFactory.CreateClientMessenger("SampleMessenger", handler);

            Console.WriteLine("Initializing client messenger");
            _messenger.Init(_cancellationToken);

            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }

        static Task SendWithoutResponseAsync(byte[] payload)
        {
            return _messenger.SendWithoutResponseAsync(payload);
        }
    }
}
