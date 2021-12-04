using GoXLR.Server.Commands;
using GoXLR.Server.Configuration;
using GoXLR.Server.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class RoutingTableSettingsEventHandler : INotificationHandler
    {
        private readonly GoXLRState _state;
        private readonly ILogger<RoutingTableSettingsEventHandler> _logger;

        public RoutingTableSettingsEventHandler(
            GoXLRState state,
            ILogger<RoutingTableSettingsEventHandler> logger)
        {
            _state = state;
            _logger = logger;
        }

        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (message.Event != "getSettings")
                return;

            if (message.Action != "com.tchelicon.goxlr.routingtable")
                return;

            _logger.LogInformation("RoutingTableSettingsEventHandler");

            if (!Routing.TryParseContext(message.Context, out var routing))
                return;

            //Part of the registration chain when registering a state subscription event:
            await _state.CommandHandler.Send(new RespondCanReceiveRoutingStateCommand(message.Context, routing));
        }
    }
}
