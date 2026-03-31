namespace TightWiki.ViewModels.Admin
{
    public class AddEmojiViewModel
        : TwViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OriginalName { get; set; }
        public string Categories { get; set; } = string.Empty;
    }
}
