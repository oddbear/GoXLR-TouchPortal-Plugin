namespace GoXLR.Plugin.Models
{
    public class InfoMessage : BaseMessage
    {
        public string TpVersionString { get; set; }
        public string PluginVersion { get; set; }
        public string TpVersionCode { get; set; }
        public string SdkVersion { get; set; }
        public string Status { get; set; }
    }
}
