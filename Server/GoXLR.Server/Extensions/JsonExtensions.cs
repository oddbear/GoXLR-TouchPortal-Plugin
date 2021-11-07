using System.Linq;
using System.Text.Json;
using GoXLR.Server.Enums;
using GoXLR.Server.Models;

namespace GoXLR.Server.Extensions
{
    public static class JsonExtensions
    {

        public static Profile[] GetProfilesFromPayload(this JsonElement jsonElement)
        {
            return jsonElement
                .GetProperty("payload")
                .GetProperty("Profiles")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Select(profileName => new Profile(profileName))
                .ToArray();
        }

        public static State GetStateFromPayload(this JsonElement jsonElement)
        {
            return (State)jsonElement
                .GetProperty("payload")
                .GetProperty("state")
                .GetInt32();
        }

        public static string GetContext(this JsonElement jsonElement)
        {
            return jsonElement.TryGetProperty("context", out var value)
                ? value.GetString()
                : default;
        }

        public static string GetAction(this JsonElement jsonElement)
        {
            return jsonElement.TryGetProperty("action", out var value)
                ? value.GetString()
                : default;
        }

        public static string GetEvent(this JsonElement jsonElement)
        {
            return jsonElement.TryGetProperty("event", out var value)
                ? value.GetString()
                : default;
        }
    }
}