using System.Text.Json;

namespace GoXLR.Server.Handlers
{
    public class MessageNotification
    {
        public string Action { get; set; }
        public string Context { get; set; }
        public string Event { get; set; }
        public JsonElement Payload { get; set; }
    }
}
