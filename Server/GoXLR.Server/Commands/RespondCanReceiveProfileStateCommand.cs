using System.Text.Json;

namespace GoXLR.Server.Commands
{
    internal class RespondCanReceiveProfileStateCommand : CommandBase
    {
        public RespondCanReceiveProfileStateCommand(string profileName)
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = profileName,
                @event = "didReceiveSettings",
                payload = new
                {
                    settings = new
                    {
                        SelectedProfile = profileName
                    }
                }
            });

            Json = new [] { json };
        }
    }
}
