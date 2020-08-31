﻿using System;
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
        void Init(CancellationToken? cancellationToken = null);

        /// <summary>
        /// Sends a message (fire and forget).
        /// </summary>
        /// <param name="payload">Message payload</param>
        Task SendAsync(byte[] payload);

        /// <summary>
        /// Sends a request message and waits for its response.
        /// </summary>
        /// <param name="payload">Request message payload</param>
        /// <returns>Response message payload</returns>
        Task<byte[]> SendRequestAsync(byte[] payload);
    }
}
