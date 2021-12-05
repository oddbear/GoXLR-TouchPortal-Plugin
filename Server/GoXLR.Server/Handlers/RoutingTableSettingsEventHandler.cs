using GoXLR.Server.Commands;
using GoXLR.Server.Handlers.Attributes;
using GoXLR.Server.Handlers.Interfaces;
using GoXLR.Server.Handlers.Models;
using GoXLR.Server.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoXLR.Server.Handlers
{
    public class RoutingTableSettingsEventHandler : INotificationHandler
    {
        private readonly CommandHandler _commandHandler;

        public RoutingTableSettingsEventHandler(CommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        [Event("getSettings"), Action("com.tchelicon.goxlr.routingtable")]
        public async Task Handle(MessageNotification message, CancellationToken cancellationToken)
        {
            if (!Routing.TryParseContext(message.Context, out var routing))
                return;

            //Part of the registration chain when registering a state subscription event:
            await _commandHandler.Send(new RespondCanReceiveRoutingStateCommand(message.Context, routing), cancellationToken);
        }
    }
}
