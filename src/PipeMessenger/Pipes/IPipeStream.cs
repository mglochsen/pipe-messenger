using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal interface IPipeStream : IDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null);

        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null);
    }
}
