using System.ComponentModel;

namespace GoXLR.Server.Enums
{
    public enum RouteOutput
    {
        [Description("Headphones")]
        Headphones,

        [Description("Broadcast Mix")]
        BroadcastMix,

        [Description("Line Out")]
        LineOut,

        [Description("Chat Mic")]
        ChatMic,

        [Description("Sampler")]
        Sampler
    }
}