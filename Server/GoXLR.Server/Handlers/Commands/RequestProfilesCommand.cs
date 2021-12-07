using System.Text.Json;

namespace GoXLR.Server.Handlers.Commands
{
    /// <summary>
    /// Fetching profiles from the selected GoXLR App.
    /// </summary>
    internal class RequestProfilesCommand : CommandBase
    {
        public RequestProfilesCommand()
        {
            var json = JsonSerializer.Serialize(new
            {
                action = "com.tchelicon.goxlr.profilechange",
                context = "fetchingProfiles",
                @event = "propertyInspectorDidAppear"
            });

            Json = new[] { json };
        }
    }
}
