using System.Text.Json.Serialization;
using GoXLR.Models.Models.Payloads;
using GoXLR.Models.Models.Shared;

namespace GoXLR.Models.Models
{
    public class SetProfileRequest : RequestModelBase
    {
        [JsonPropertyName("payload")]
        public SetProfilePayload Payload { get; set; }

        public static SetProfileRequest Create(string profile)
        {
            var settings = new SetProfilePayload.SetProfileSettings
            {
                SelectedProfile = profile
            };

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
                    Settings = settings
                }
            };
        }
    }
}
