using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMessenger
{
    /// <summary>
    /// A Messenger can be used to send messages.
    /// </summary>
    public interface IMessenger : IDisposable
    {
        /// <summary>
        /// Gets a value determining if the messenger is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initializes the messenger.
        /// </summary>
        Task InitAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        /// Sends a message (fire and forget).
        /// </summary>
        /// <param name="payload">Message payload</param>
        Task<bool> SendAsync(byte[] payload);

        /// <summary>
        /// Sends a request message and returns a id to be able to identify its response.
        /// </summary>
        /// <param name="payload">Request message payload</param>
        /// <returns>Id of response</returns>
        Task<Guid?> SendRequestAsync(byte[] payload);
    }
}
