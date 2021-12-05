using GoXLR.Server.Enums;
using GoXLR.Server.Models;
using System.Linq;
using System.Text.Json;

namespace GoXLR.Server.Handlers.Models
{
    public class MessageNotification
    {
        public string Action { get; set; }
        public string Context { get; set; }
        public string Event { get; set; }
        public JsonElement Payload { get; set; }

        public State GetStateFromPayload()
        {
            return (State)Payload
                .GetProperty("state")
                .GetInt32();
        }

        public Profile[] GetProfilesFromPayload()
        {
            return Payload
                .GetProperty("Profiles")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Select(profileName => new Profile(profileName))
                .ToArray();
        }
    }
}
