using System.Text.Json.Serialization;

namespace GoXLR_TouchPortal_Plugin.Models
{
    public class SetRoutingRequest
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        //Id of the button?
        [JsonPropertyName("context")]
        public string Context { get; set; }

        //Id of the device streamdeck / touchportal / loupdeck etc. ?
        [JsonPropertyName("device")]
        public string Device { get; set; }

        //Guess action happens not on press, but on release of the button (tested from phone)
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("payload")]
        public SetRoutingPayload Payload { get; set; }

        public static SetRoutingRequest Create(SetRoutingSettings settings)
        {
            return new SetRoutingRequest
            {
                Action = "com.tchelicon.goxlr.routingtable",
                Context = "00000000000000000000000000000000",
                Device = "00000000000000000000000000000000",
                Event = "keyUp", 
                Payload = new SetRoutingPayload
                {
                    Coordinates = new Coordinates
                    {
                        Column = 0, 
                        Row = 0 
                    },
                    IsInMultiAction = false,
                    Settings = settings
                }
            };
        }

        public class SetRoutingPayload
        {
            [JsonPropertyName("coordinates")]
            public Coordinates Coordinates { get; set; }

            //Is the button in a multiaction?
            [JsonPropertyName("isInMultiAction")]
            public bool IsInMultiAction { get; set; }

            [JsonPropertyName("settings")]
            public SetRoutingSettings Settings { get; set; }
        }

        public class Coordinates
        {
            //Coordinates of the button on the screen, from 0-x
            [JsonPropertyName("column")]
            public int Column { get; set; }

            //Coordinates of the button on the screen, from 0-x
            [JsonPropertyName("row")]
            public int Row { get; set; }
        }

        public class SetRoutingSettings
        {
            public string RoutingAction { get; set; }

            public string RoutingInput { get; set; }

            public string RoutingOutput { get; set; }
        }
    }
}
