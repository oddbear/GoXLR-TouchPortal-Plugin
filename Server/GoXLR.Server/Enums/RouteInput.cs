using System.ComponentModel;

namespace GoXLR.Server.Enums
{
    public enum RouteInput
    {
        [Description("Mic")]
        Mic,

        [Description("Chat")]
        Chat,

        [Description("Music")]
        Music,

        [Description("Game")]
        Game,

        [Description("Console")]
        Console,

        [Description("Line In")]
        LineIn,

        [Description("System")]
        System,

        [Description("Samples")]
        Samples
    }
}
