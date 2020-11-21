using System.Text.Json.Serialization;
using GoXLR.Models.Models.Payloads;
using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models
{
    //com.tchelicon.goXLR.ChangeProfile
    //sendToPropertyInspector
    public class GetProfilesResponse : ModelBase
    {
        [JsonPropertyName("payload")]
        public GetProfilesResponsePayload Payload { get; set; }
    }
}
