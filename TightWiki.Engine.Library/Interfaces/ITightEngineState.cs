using Microsoft.AspNetCore.Http;
using TightWiki.Library.Interfaces;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface ITightEngineState
    {
        #region Parameters.

        ISessionState? Session { get; }
        IQueryCollection QueryString { get; }

        ITightEngine Engine { get; }
        IPage Page { get; }
        int? Revision { get; }

        #endregion

        #region State.

        Dictionary<string, string> Variables { get; }
        Dictionary<string, string> Snippets { get; }
        List<string> Tags { get; set; }
        List<string> ProcessingInstructions { get; }
        List<NameNav> OutgoingLinks { get; }
        List<TableOfContentsTag> TableOfContents { get; }
        List<string> Headers { get; }

        #endregion

        #region Results.

        string HtmlResult { get; }
        TimeSpan ProcessingTime { get; }
        int ErrorCount { get; }
        int MatchCount { get; }

        #endregion

        string GetNextQueryToken();
    }
}
