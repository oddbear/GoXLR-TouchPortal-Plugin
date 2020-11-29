using System.Text.Json.Serialization;
using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models.Payloads
{
    public class SetRoutingPayload : RequestPayloadBase
    {
        [JsonPropertyName("settings")]
        public SetRoutingSettings Settings { get; set; }

        public class SetRoutingSettings
        {
            public string RoutingAction { get; set; }

            public string RoutingInput { get; set; }

            public string RoutingOutput { get; set; }
        }
    }
}