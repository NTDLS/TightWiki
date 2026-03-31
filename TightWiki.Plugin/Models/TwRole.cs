namespace TightWiki.Plugin.Models
{
    public partial class TwRole
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; }
    }
}
