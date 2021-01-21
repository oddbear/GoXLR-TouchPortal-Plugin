using GoXLR.Shared.Models;

namespace GoXLR.Plugin
{
    public class AppSettings
    {
        public WebSocketServerSettings WebSocketServerSettings { get; set; }

        //Put application specific stuff here.
        public string ReconnectWaitTime { get; set; } = "00:00:05";
    }
}
