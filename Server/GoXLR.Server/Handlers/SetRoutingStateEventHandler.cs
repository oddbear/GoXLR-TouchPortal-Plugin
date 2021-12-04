using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class SetRoutingStateEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<SetRoutingStateEventHandler> _logger;

        public SetRoutingStateEventHandler(
            GoXLRState state,
            ILogger<SetRoutingStateEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (message.Event != "setState")
                return;

            if (message.Action != "com.tchelicon.goxlr.routingtable")
                return;

            _logger.LogInformation("SetRoutingStateEventHandler");

            var routingState = message.Payload.GetStateFromPayload();

            if (!Routing.TryParseContext(message.Context, out var routing))
                return;

            _state.EventHandler.RoutingStateChangedEvent(routing, routingState);
        }
    }
}
