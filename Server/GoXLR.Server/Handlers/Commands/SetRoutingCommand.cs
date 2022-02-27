using System.Text.Json;
using GoXLR.Server.Enums;
using GoXLR.Server.Extensions;
using GoXLR.Server.Models;

namespace GoXLR.Server.Handlers.Commands
{
    internal class SetRoutingCommand : CommandBase
    {
        public SetRoutingCommand(RoutingAction action, Routing routing)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.routingtable",
                @event = "keyUp",
                payload = new
                {
                    settings = new
                    {
                        RoutingAction = action.GetEnumDescription(),
                        RoutingInput = routing.Input.GetEnumDescription(),
                        RoutingOutput = routing.Output.GetEnumDescription()
                    }
                }
            });

            Json = new[] { json };
        }
    }
}
