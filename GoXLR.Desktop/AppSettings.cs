namespace GoXLR.Desktop
{
    public class AppSettings
    {
        public WebSocketServerSettings WebSocketServerSettings { get; set; }
        public bool DebugConsole { get; set; }
    }

    public class WebSocketServerSettings
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 6805;
    }
}
