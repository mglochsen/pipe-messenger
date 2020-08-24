using System;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Sample.Common;

namespace PipeMessenger.Sample.Client
{
    class Program
    {
        static IMessenger _messenger;
        static CancellationToken _cancellationToken;

        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = cancellationTokenSource.Token;

            Console.WriteLine("Creating client messenger");
            var handler = new DefaultMessageHandler();
            _messenger = MessengerFactory.CreateClientMessenger("SampleMessenger", handler, true);

            Console.WriteLine("Initializing client messenger");
            _messenger.Init(_cancellationToken);

            var consoleMessenger = new ConsoleMessenger();
            await consoleMessenger.StartAsync(_messenger);

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }
    }
}
