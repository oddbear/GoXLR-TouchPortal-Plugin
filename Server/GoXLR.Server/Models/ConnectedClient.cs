namespace GoXLR.Server.Models
{
    public record ConnectedClient(string Name)
    {
        public static ConnectedClient Empty => new(string.Empty);
    }
}
