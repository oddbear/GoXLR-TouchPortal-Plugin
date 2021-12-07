using System.Text.Json;
using GoXLR.Server.Models;

namespace GoXLR.Server.Handlers.Commands
{
    internal class SetProfileCommand : CommandBase
    {
        public SetProfileCommand(Profile profile)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                @event = "keyUp",
                payload = new
                {
                    settings = new
                    {
                        SelectedProfile = profile.Name
                    }
                }
            });

            Json = new[] { json };
        }
    }
}
