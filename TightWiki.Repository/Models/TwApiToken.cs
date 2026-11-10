namespace TightWiki.Repository.Models
{
    /// <summary>
    /// A personal API token as shown to its owner. The token itself and its hash are never exposed.
    /// </summary>
    public class TwApiToken
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
