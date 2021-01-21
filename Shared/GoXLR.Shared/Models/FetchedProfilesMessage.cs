namespace GoXLR.Shared.Models
{
    public record FetchedProfilesMessage(ClientIdentifier ClientIpAddress, string InstanceId, string[] Profiles);
}
