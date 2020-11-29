using System.Text.Json.Serialization;
using GoXLR.Models.Models.Payloads;
using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models
{
    public class SetRoutingRequest : RequestModelBase
    {
        [JsonPropertyName("payload")]
        public SetRoutingPayload Payload { get; set; }

        public static SetRoutingRequest Create(SetRoutingPayload.SetRoutingSettings settings)
        {
            return new SetRoutingRequest
            {
                Action = "com.tchelicon.goxlr.routingtable",
                Context = "00000000000000000000000000000000",
                Device = "00000000000000000000000000000000",
                Event = "keyUp", 
                Payload = new SetRoutingPayload
                {
                    Coordinates = new Coordinates { Column = 0, Row = 0  },
                    IsInMultiAction = false,
                    Settings = settings
                }
            };
        }
    }
}
