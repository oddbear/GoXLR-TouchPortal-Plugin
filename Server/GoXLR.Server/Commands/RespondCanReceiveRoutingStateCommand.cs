using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;
using System.Text.Json;

namespace GoXLR.Server.Commands
{
    internal class RespondCanReceiveRoutingStateCommand : CommandBase
    {
        public RespondCanReceiveRoutingStateCommand(string context, Routing routing)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.routingtable",
                context = context,
                @event = "didReceiveSettings",
                payload = new
                {
                    settings = new
                    {
                        RoutingAction = RoutingAction.Toggle.ToString(),
                        RoutingInput = routing.Input.GetEnumDescription(),
                        RoutingOutput = routing.Output.GetEnumDescription()
                    }
                }
            });

            Json = new [] { json };
        }
    }
}
