﻿using System;

namespace PipeMessenger.Contracts
{
    public interface IMessageHandler : IDisposable
    {
        void OnConnected();

        void OnDisconnected();

        void OnMessage(byte[] payloadBytes);

        byte[] OnRequestMessage(byte[] payloadBytes);
    }
}
