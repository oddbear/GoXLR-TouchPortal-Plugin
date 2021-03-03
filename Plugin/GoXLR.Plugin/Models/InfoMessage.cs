namespace GoXLR.Plugin.Models
{
    public record InfoMessage(
        string Type,
        string TpVersionString,
        int PluginVersion,
        int TpVersionCode,
        int SdkVersion,
        string Status
    ) : BaseMessage(Type);
}
