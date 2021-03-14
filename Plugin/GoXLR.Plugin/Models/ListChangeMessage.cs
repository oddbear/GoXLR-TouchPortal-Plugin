namespace GoXLR.Plugin.Models
{
    public record ListChangeMessage(
        string Type,
        string PluginId,
        string ActionId,
        string ListId,
        string InstanceId,
        string Value
    ) : BaseMessage(Type);
}
