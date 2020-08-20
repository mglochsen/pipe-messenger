using System;
using System.Threading;
using PipeMessenger.Sample.Common;

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
            var messenger = MessengerFactory.CreateServerMessenger("SampleMessenger", new JsonMessageSerializer());
            var handler = new ServerMessageHandler(payloadBytes => messenger.SendRequestAsync(payloadBytes));
            _messenger = messenger;
            
            Console.WriteLine("Initializing server messenger");
            _messenger.Init(handler, _cancellationToken);
            
            Console.WriteLine("[Press enter to exit]");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }
    }
}
