using GoXLR.Server.Commands;
using GoXLR.Server.Models;

namespace GoXLR.Server.Handlers
{
    internal static class RoutingTableSettingsEvent
    {
        public static void Handle(CommandHandler commandHandler, string propertyContext)
        {
            if (!Routing.TryParseContext(propertyContext, out var routing))
                return;

            //Part of the registration chain when registering a state subscription event:
            commandHandler.Send(new RespondCanReceiveRoutingStateCommand(propertyContext, routing));
        }
    }
}
