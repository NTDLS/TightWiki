using Microsoft.AspNetCore.Http;

namespace TightWiki.Library.Interfaces
{
    public interface IWikiContext
    {
        IQueryCollection? QueryString { get; set; }
        public bool CanCreate { get; }
        public DateTime LocalizeDateTime(DateTime dateTime);
    }
}
