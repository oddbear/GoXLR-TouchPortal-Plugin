namespace GoXLR.Plugin.Models
{
    public record InfoMessage(
        string Type,
        string TpVersionString,
        string PluginVersion,
        string TpVersionCode,
        string SdkVersion,
        string Status
    ) : BaseMessage(Type);
}
