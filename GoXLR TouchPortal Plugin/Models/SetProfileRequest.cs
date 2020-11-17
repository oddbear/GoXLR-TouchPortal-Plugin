using System.Text.Json.Serialization;

namespace GoXLR_TouchPortal_Plugin.Models
{
    public class SetProfileRequest
    {
        //com.tchelicon.goxlr.profilechange
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("context")]
        public string Context { get; set; }

        [JsonPropertyName("device")]
        public string Device { get; set; }

        //sendToPropertyInspector
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("payload")]
        public SetProfilePayload Payload { get; set; }

        public static SetProfileRequest Create(string profile)
        {
            return new SetProfileRequest
            {
                Action = "com.tchelicon.goxlr.profilechange",
                Context = "00000000000000000000000000000000",
                Device = "00000000000000000000000000000000",
                Event = "keyUp",
                Payload = new SetProfilePayload
                {
                    Coordinates = new Coordinates { Column = 0, Row = 0 },
                    IsInMultiAction = false,
                    Settings = new SetProfileSettings
                    {
                        SelectedProfile = profile
                    }
                }
            };
        }

        public class SetProfilePayload
        {
            [JsonPropertyName("coordinates")]
            public Coordinates Coordinates { get; set; }

            [JsonPropertyName("isInMultiAction")]
            public bool IsInMultiAction { get; set; }

            [JsonPropertyName("settings")]
            public SetProfileSettings Settings { get; set; }
        }

        public class Coordinates
        {
            [JsonPropertyName("column")]
            public int Column { get; set; }

            [JsonPropertyName("row")]
            public int Row { get; set; }
        }

        public class SetProfileSettings
        {
            public string SelectedProfile { get; set; }
        }
    }
}
