﻿using System;
using System.Text;
using System.Threading.Tasks;

namespace PipeMessenger.Sample.Common
{
    public class ConsoleMessenger
    {
        private IMessenger _messenger;

        public async Task StartAsync(IMessenger messenger)
        {
            _messenger = messenger;

            bool exitProgram = false;
            do
            {
                Console.WriteLine("[Press <Esc> to exit, <m> to send a message or <r> to send a request.]");
                var input = Console.ReadKey();
                switch (input.Key)
                {
                    case ConsoleKey.Escape:
                        exitProgram = true;
                        break;
                    case ConsoleKey.M:
                        await SendMessageAsync();
                        break;
                    case ConsoleKey.R:
                        await SendRequestAsync();
                        break;
                }

            } while (!exitProgram);
        }

        private async Task SendMessageAsync()
        {
            if (!_messenger.IsConnected)
            {
                Console.WriteLine("Unable to send a message: Messenger is not connected.");
                return;
            }

            var content = $"Hello at {DateTime.Now.TimeOfDay}";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            Console.WriteLine("Sending message");
            await _messenger.SendWithoutResponseAsync(contentBytes);
            Console.WriteLine("Message sent");
        }

        private async Task SendRequestAsync()
        {
            var content = $"Sample request {Guid.NewGuid()}";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            Console.WriteLine("Sending request");
            var responseBytes = await _messenger.SendRequestAsync(contentBytes);
            var response = Encoding.UTF8.GetString(responseBytes);
            Console.WriteLine($"Returned response: {response}");
        }
    }
}
