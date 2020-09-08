using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal interface IPipeStream : IObservable<byte[]>, IDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task<bool> WriteAsync(byte[] data, CancellationToken? cancellationToken = null);
    }
}
