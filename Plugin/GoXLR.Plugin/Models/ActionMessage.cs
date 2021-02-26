namespace GoXLR.Plugin.Models
{
    public class ActionMessage : BaseMessage
    {
        public string PluginId { get; set; }
        public string ActionId { get; set; }
        public ActionData[] Data { get; set; }
    }

    public class ActionData
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
}
