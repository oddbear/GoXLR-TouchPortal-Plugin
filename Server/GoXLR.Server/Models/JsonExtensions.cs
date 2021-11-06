using System.Linq;
using System.Text.Json;

namespace GoXLR.Server.Models
{
    public static class JsonExtensions
    {

        public static string[] GetProfilesFromPayload(this JsonElement jsonElement)
        {
            return jsonElement
                .GetProperty("payload")
                .GetProperty("Profiles")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToArray();
        }

        public static int GetStateFromPayload(this JsonElement jsonElement)
        {
            return jsonElement
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