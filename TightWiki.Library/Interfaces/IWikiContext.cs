namespace TightWiki.Library
{
    public interface IWikiContext
    {
        public bool CanCreate { get; }
        public DateTime LocalizeDateTime(DateTime dateTime);
    }
}
