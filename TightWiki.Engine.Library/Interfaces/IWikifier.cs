using Microsoft.AspNetCore.Http;
using TightWiki.Library.Interfaces;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface IWikifier
    {
        IWikifierFactory Factory { get; }
        Dictionary<string, string> Variables { get; }
        Dictionary<string, string> Snippets { get; }
        List<string> Tags { get; set; }
        IPage Page { get; }
        int? Revision { get; }
        IQueryCollection QueryString { get; }
        ISessionState? SessionState { get; }
        List<string> ProcessingInstructions { get; }
        List<NameNav> OutgoingLinks { get; }
        string BodyResult { get; }
        List<TableOfContentsTag> TableOfContents { get; }
        List<string> Headers { get; }

        TimeSpan ProcessingTime { get; }
        int ErrorCount { get; }
        int MatchCount { get; }

        string CreateNextQueryToken();
    }
}
