namespace GoXLR.Server.Models
{
    public record Profile(string Name)
    {
        public static Profile Empty => new (string.Empty);
    }
}
