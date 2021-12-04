using GoXLR.Server.Commands;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class GoxlrConnectionEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<GoxlrConnectionEventHandler> _logger;

        public GoxlrConnectionEventHandler(GoXLRState state,
            ILogger<GoxlrConnectionEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Event != "goxlrConnectionEvent")
                return;

            _logger.LogInformation("GoxlrConnectionEventHandler");

            await _state.CommandHandler.Send(new SubscribeToRoutingStatesCommand());
            await _state.CommandHandler.Send(new RequestProfilesCommand());
        }
    }
}
