using GoXLR.Server.Models;
using System.Text.Json;

namespace GoXLR.Server.Handlers.Commands
{
    internal class SubscribeToProfileStateCommand : CommandBase
    {
        public SubscribeToProfileStateCommand(Profile profile)
        {
            //It's required to send a propertyInspectorDidAppear at least once, not sure why.
            var propertyInspectorDidAppear = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = profile.Name,
                @event = "propertyInspectorDidAppear"
            });
            var willAppear = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = profile.Name,
                @event = "willAppear"
            });

            Json = new [] { propertyInspectorDidAppear, willAppear };
        }
    }
}
