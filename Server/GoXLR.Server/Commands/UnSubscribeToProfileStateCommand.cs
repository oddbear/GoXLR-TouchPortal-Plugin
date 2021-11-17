using GoXLR.Server.Models;
using System.Text.Json;

namespace GoXLR.Server.Commands
{
    internal class UnSubscribeToProfileStateCommand : CommandBase
    {
        public UnSubscribeToProfileStateCommand(Profile profile)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = profile.Name,
                @event = "willDisappear"
            });

            Json = new [] { json };
        }
    }
}
