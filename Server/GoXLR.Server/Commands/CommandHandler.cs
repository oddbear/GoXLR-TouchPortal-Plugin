using Fleck;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

        public async Task Send(CommandBase command)
        {
            foreach (var json in command.Json)
            {
                await Send(json);
            }
        }

        public async Task Send(string message)
        {
            if (_socket?.IsAvailable != true)
                return;

            _logger.LogWarning("Send message: " + message);
            await _socket?.Send(message);
        }
    }
}
