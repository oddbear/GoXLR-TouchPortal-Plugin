using System.Text.Json.Serialization;

namespace GoXLR.Models.Models.Shared
{
    public class ModelBase
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("context")]
        public string Context { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }
    }
}
