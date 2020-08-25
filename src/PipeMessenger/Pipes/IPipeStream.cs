using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger.Pipes
{
    internal interface IPipeStream : IDisposable
    {
        bool IsConnected { get; }

        PipeTransmissionMode ReadMode { get; set; }

        bool IsMessageComplete { get; }

        Task ConnectAsync(CancellationToken cancellationToken);

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null);

        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken? cancellationToken = null);
    }
}
