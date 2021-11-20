using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using System.Text.Json;

namespace GoXLR.Server.Handlers
{
    internal static class SetRoutingStateEvent
    {
        public static void Handle(IGoXLREventHandler eventHandler, JsonElement root)
        {
            if (eventHandler is null)
                return;

            var propertyContext = root.GetContext();
            var routingState = root.GetStateFromPayload();

            if (!Routing.TryParseContext(propertyContext, out var routing))
                return;

            eventHandler.RoutingStateChangedEvent(routing, routingState);

        }
    }
}
