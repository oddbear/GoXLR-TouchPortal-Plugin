namespace GoXLR.Plugin.Models
{
    public class ListChangeMessage : BaseMessage
    {
        public string PluginId { get; set; }
        public string ActionId { get; set; }
        public string ListId { get; set; }
        public string InstanceId { get; set; }
        public string Value { get; set; }
    }
}
