using GoXLR.Server.Configuration;
using GoXLR.Server.Models;

namespace GoXLR.TouchPortal.Plugin
{
    public class AppSettings
    {
        public WebSocketServerSettings WebSocketServerSettings { get; set; }

        public TouchPortalClientSettings TouchPortalClientSettings { get; set; }
    }

    public class TouchPortalClientSettings
    {
        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 12136;
        public string PluginId { get; set; } = "oddbear.touchportal.goxlr";

        public string InfoMessageTimeout { get; set; } = "00:00:05";
    }
}
