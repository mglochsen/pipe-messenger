using System;
using System.Threading;
using System.Threading.Tasks;
using PipeMessenger.Sample.Common;

namespace PipeMessenger.Sample.Server
{
    class Program
    {
        static IMessenger _messenger;

        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Console.WriteLine("Creating server messenger");
            var handler = new DefaultMessageHandler();
            _messenger = MessengerFactory.CreateServerMessenger("SampleMessenger", handler, true);
            
            Console.WriteLine("Initializing server messenger");
            _messenger.Init(cancellationToken);

            var consoleMessenger = new ConsoleMessenger();
            await consoleMessenger.StartAsync(_messenger);

            cancellationTokenSource.Cancel();
            _messenger.Dispose();
            _messenger = null;
        }
    }
}
