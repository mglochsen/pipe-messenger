using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal interface IPipe : IDisposable
    {
        bool IsConnected { get; }

        void Init(Action connectedAction, CancellationToken cancellationToken);

        void StartPipeObservation(Action<byte[]> dataReceivedAction, Action disconnectedAction);

        void Reconnect(Action connectedAction, Action disconnectedAction, Action<byte[]> dataReceivedAction);

        Task WriteAsync(byte[] data);
    }
}