using System.Text.Json.Serialization;

namespace GoXLR_TouchPortal_Plugin.Models
{
    public class GetProfilesRequest
    {
        //com.tchelicon.goXLR.ChangeProfile
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("context")]
        public string Context { get; set; }

        //sendToPropertyInspector
        [JsonPropertyName("event")]
        public string Event { get; set; }

        public static GetProfilesRequest Create()
        {
            return new GetProfilesRequest
            {
                Action = "com.tchelicon.goxlr.profilechange",
                Context = "00000000000000000000000000000000",
                Event = "propertyInspectorDidAppear"
            };
        }
    }
}
