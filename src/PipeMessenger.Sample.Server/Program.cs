using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Sample.Server
{
    class Program
    {
        static Messenger _messenger;
        static CancellationToken _cancellationToken;

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = cancellationTokenSource.Token;

            Console.WriteLine("Creating server messenger");
            var handler = new ServerMessageHandler(SendRequestAsync);
            _messenger = MessengerFactory.CreateServerMessenger("SampleMessenger", handler, true);
            
            Console.WriteLine("Initializing server messenger");
            _messenger.Init(_cancellationToken);
            
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }

        static Task<byte[]> SendRequestAsync(byte[] payloadBytes)
        {
            return _messenger.SendRequestAsync(payloadBytes);
        }
    }
}
