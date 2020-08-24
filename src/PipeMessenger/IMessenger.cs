using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger
{
    public interface IMessenger : IDisposable
    {
        bool IsConnected { get; }

        void Init(CancellationToken? cancellationToken = null);

        Task SendWithoutResponseAsync(byte[] payload);

        Task<byte[]> SendRequestAsync(byte[] payload);
    }
}
