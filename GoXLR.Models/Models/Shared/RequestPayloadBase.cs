using System.Text.Json.Serialization;

namespace GoXLR.Models.Models.Shared
{
    //Is this for statistics?
    public class RequestPayloadBase
    {
        //The placement of the button on the controller device.
        [JsonPropertyName("coordinates")]
        public Coordinates Coordinates { get; set; }

        //If it's in a multi action group on the device.
        [JsonPropertyName("isInMultiAction")]
        public bool IsInMultiAction { get; set; }
    }
}
