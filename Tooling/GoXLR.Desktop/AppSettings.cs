using GoXLR.Shared.Models;

namespace GoXLR.Desktop
{
    public class AppSettings
    {
        public WebSocketServerSettings WebSocketServerSettings { get; set; }

        //Put application specific stuff here.
        public bool DebugConsole { get; set; }
    }
}
