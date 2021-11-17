using GoXLR.Server.Models;
using System.Linq;
using System.Text.Json;

namespace GoXLR.Server.Commands
{
    internal class SubscribeToRoutingStatesCommand : CommandBase
    {
        public SubscribeToRoutingStatesCommand()
        {
            //This is a response to the connected event.
            //We can register subscription to all possible routing combinations, as this is fixed.
            //We cannot do this for profiles yet, as this is a list that might change.
            //Therefor we need to do this after we have requested the list of the profiles.
            var json = JsonSerializer.Serialize(new
            {
                @event = "goxlrConnectionEvent",
                payload = Routing.GetRoutingTable()
                    .Select(routing => new
                    {
                        action = "com.tchelicon.goxlr.routingtable",
                        context = $"{routing.Input}{GoXLRServer.RoutingSeparator}{routing.Output}"
                    })
                    .ToArray()
            });

            Json = new [] { json };
        }
    }
}
