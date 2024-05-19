namespace TightWiki.Library.DataModels
{
    public class PageInfoAndHash
    {
        public int Revision { get; set; }
        public int DataHash { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
