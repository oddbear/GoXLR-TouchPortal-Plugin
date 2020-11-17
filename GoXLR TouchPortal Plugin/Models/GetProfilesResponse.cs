using System.Text.Json.Serialization;

namespace GoXLR_TouchPortal_Plugin.Models
{
    public class GetProfilesResponse
    {
        //com.tchelicon.goXLR.ChangeProfile
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("context")]
        public string Context { get; set; }
        
        //sendToPropertyInspector
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("payload")]
        public GetProfilesResponsePayLoad Payload { get; set; }
    }

    public class GetProfilesResponsePayLoad
    {
        public string[] Profiles { get; set; }
    }
}
