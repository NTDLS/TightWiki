using Microsoft.AspNetCore.Http;

namespace TightWiki.Library
{
    public interface IWikiContext
    {
        IQueryCollection? QueryString { get; set; }
        public bool CanCreate { get; }
        public DateTime LocalizeDateTime(DateTime dateTime);
    }
}
