namespace TightWiki.Shared.Models.View
{
    public class AddEmojiModel : ModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Categories { get; set; }
    }
}
