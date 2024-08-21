using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

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
        public HashSet<WikiMatchType> OmitMatches { get; }
        public int NestDepth { get; } //Used for recursion.

        #endregion

        #region State.

        /// <summary>
        /// Custom page title set by a call to @@Title("...")
        /// </summary>
        public string? PageTitle { get; set; }

        Dictionary<string, string> Variables { get; }
        Dictionary<string, string> Snippets { get; }
        List<string> Tags { get; set; }
        List<string> ProcessingInstructions { get; }
        List<PageReference> OutgoingLinks { get; }
        List<TableOfContentsTag> TableOfContents { get; }
        List<string> Headers { get; }

        #endregion

        #region Results.

        string HtmlResult { get; }
        TimeSpan ProcessingTime { get; }
        int ErrorCount { get; }
        int MatchCount { get; }

        #endregion

        /// <summary>
        /// Used to store values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public void SetStateValue<T>(string key, T value);

        /// <summary>
        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public bool TryGetStateValue<T>(string key, [MaybeNullWhen(false)] out T? outValue);

        /// <summary>
        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        /// </summary>
        public T GetStateValue<T>(string key, T defaultValue);

        string GetNextQueryToken();

        /// <summary>
        /// Transforms "included" wiki pages, for example if a wiki function
        /// injected additional wiki markup, this 'could' be processed separately.
        /// </summary>
        /// <param name="page">The child page to process</param>
        /// <param name="revision">The optional revision of the child page to process</param>
        /// <returns></returns>
        ITightEngineState TransformChild(IPage page, int? revision = null);
    }
}
