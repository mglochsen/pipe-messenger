using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal interface IPipe : IDisposable
    {
        bool IsConnected { get; }

        void Init(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction, CancellationToken cancellationToken);

        Task WriteAsync(byte[] data);
    }
}