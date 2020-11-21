using System.Text.Json.Serialization;

namespace GoXLR.Models.Models.Shared
{
    public class RequestModelBase : ModelBase
    {
        //The id of the controller device?
        [JsonPropertyName("device")]
        public string Device { get; set; }
    }
}
