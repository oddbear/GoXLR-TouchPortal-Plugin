namespace GoXLR.Shared.Models
{
    public record FetchedProfilesMessage(ClientIdentifier ClientIdentifier, string InstanceId, string[] Profiles);
}
