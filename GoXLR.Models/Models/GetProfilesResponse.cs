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

        public static GetProfilesResponse Create(string[] profiles)
        {
            return new GetProfilesResponse
            {
                Action = "com.tchelicon.goXLR.ChangeProfile",
                Context = "00000000000000000000000000000000",
                Event = "sendToPropertyInspector",
                Payload = new GetProfilesResponsePayload
                {
                    Profiles = profiles
                }
            };
        }
    }
}
