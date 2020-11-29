using System.Text.Json.Serialization;
using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models.Payloads
{
    public class SetProfilePayload : RequestPayloadBase
    {
        [JsonPropertyName("settings")]
        public SetProfileSettings Settings { get; set; }

        public class SetProfileSettings
        {
            public string SelectedProfile { get; set; }
        }
    }
}