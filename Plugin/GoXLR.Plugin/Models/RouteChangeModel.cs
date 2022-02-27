using System;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using GoXLR.TouchPortal.Plugin.Configuration;
using TouchPortalSDK.Messages.Events;

namespace GoXLR.TouchPortal.Plugin.Models
{
    public class RouteChangeModel
    {
        public Routing Routing { get; }
        
        public RoutingAction RoutingAction { get; }
        
        private RouteChangeModel(RoutingAction routingAction, Routing routing)
        {
            RoutingAction = routingAction;
            Routing = routing;
        }

        public static bool TryParse(ActionEvent message, out RouteChangeModel routeChangeModel)
        {
            routeChangeModel = default;

            try
            {
                if (message is null)
                    return false;

                var action = message[Identifiers.RoutingTableChangeRequestedId + ".data.actions"];

                if (!EnumExtensions.TryParseEnumFromDescription<RoutingAction>(action, out var routingAction))
                    return false;

                var input = message[Identifiers.RoutingTableChangeRequestedId + ".data.inputs"];
                var output = message[Identifiers.RoutingTableChangeRequestedId + ".data.outputs"];

                if (!Routing.TryParseDescription(input, output, out var routing))
                    return false;

                routeChangeModel = new RouteChangeModel(routingAction, routing);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
