using System.Text.Json.Serialization;

namespace GoXLR.Models.Models.Shared
{
    public class Coordinates
    {
        [JsonPropertyName("column")]
        public int Column { get; set; }

        [JsonPropertyName("row")]
        public int Row { get; set; }
    }
}