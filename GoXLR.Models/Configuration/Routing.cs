namespace GoXLR.Models.Configuration
{
    public static class Routing
    {
        public static readonly string[] Inputs = { "Mic", "Chat", "Music", "Game", "Console", "Line In", "System", "Samples" };
        public static readonly string[] Outputs = { "Headphones", "Broadcast Mix", "Line Out", "Chat Mic", "Sampler" };
        public static readonly string[] Actions = { "Turn On", "Turn Off", "Toggle" };
    }
}
