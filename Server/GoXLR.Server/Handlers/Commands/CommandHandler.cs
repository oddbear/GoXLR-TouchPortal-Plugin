using Fleck;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers.Commands
{
    public class CommandHandler
    {
        private readonly IWebSocketConnection _socket;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(IWebSocketConnection socket, ILogger<CommandHandler> logger)
        {
            _socket = socket;
            _logger = logger;
        }

        public async Task Send(CommandBase command, CancellationToken cancelationToken)
        {
            //Some commands needs multiple payloads:
            foreach (var json in command.Json)
            {
                await Send(json, cancelationToken);
            }
        }

        private async Task Send(string message, CancellationToken cancelationToken)
        {
            if (!_socket.IsAvailable || cancelationToken.IsCancellationRequested)
                return;

            _logger.LogDebug($"Send message: {message}");

            await _socket?.Send(message);
        }
    }
}
