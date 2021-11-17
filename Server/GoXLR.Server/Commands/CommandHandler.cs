﻿using Fleck;
using Microsoft.Extensions.Logging;

namespace GoXLR.Server.Commands
{
    public class CommandHandler
    {
        private readonly IWebSocketConnection _socket;
        private readonly ILogger _logger;

        public CommandHandler(IWebSocketConnection socket, ILogger logger)
        {
            _socket = socket;
            _logger = logger;
        }

        public bool IsAvailable => _socket.IsAvailable;

        public void Send(CommandBase command)
        {
            foreach (var json in command.Json)
            {
                Send(json);
            }
        }

        public void Send(string message)
        {
            _logger.LogWarning("Send message: " + message);
            _ = _socket?.Send(message);
        }
    }
}
