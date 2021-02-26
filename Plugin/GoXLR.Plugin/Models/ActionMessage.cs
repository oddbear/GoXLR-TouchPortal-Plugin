namespace GoXLR.Plugin.Models
{
    public record ActionMessage(
        string Type,
        string PluginId,
        string ActionId,
        ActionData[] Data
    ) : BaseMessage(Type);

    public record ActionData(
        string Id,
        string Value
    );
}
