using System.Text.Json;

namespace GoXLR.Server.Models
{
    public static class JsonExtensions
    {
        public static TResult GetValue<TResult>(this JsonElement jsonElement, string propertyName)
        {
            if (jsonElement.TryGetProperty(propertyName, out var value))
            {
                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        var stringValue = value.GetString();
                        return Cast<string, TResult>(stringValue);
                }
            }

            return default;
        }

        private static TResult Cast<TValue, TResult>(TValue value)
        {
            return value is TResult result
                ? result
                : default;
        }
    }
}
