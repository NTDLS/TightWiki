using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Library.Interfaces
{
    public interface ITightEngineState
    {
        #region Parameters.

        ISharedLocalizationText Localizer { get; }
        ISessionState? Session { get; }
        IQueryCollection QueryString { get; }

        ITightEngine Engine { get; }
        IPage Page { get; }
        int? Revision { get; }
        public HashSet<WikiMatchType> OmitMatches { get; }
        public int NestDepth { get; } //Used for recursion.

        #endregion

        public ILogger<ITightEngine> Logger { get; }

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

        string GetNextHttpQueryToken();

        /// <summary>
        /// Transforms "included" wiki pages, for example if a wiki function
        /// injected additional wiki markup, this 'could' be processed separately.
        /// </summary>
        /// <param name="page">The child page to process</param>
        /// <param name="revision">The optional revision of the child page to process</param>
        Task<ITightEngineState> TransformChild(IPage page, int? revision = null);

        /// <summary>
        /// Replaces placeholders in the specified page content with previously stored match values.
        /// </summary>
        /// <param name="pageContent">The page content in which placeholders will be replaced. Cannot be null.</param>
        /// <param name="forceDecode">If true, matches are replaced even if they are set to not allow nested decode.
        /// ForceDecode is typically only executed at the end of all processing but is made available here for special use cases by custom functions.
        /// <see cref="WikiMatchSet.AllowNestedDecode"/></param>
        public void SwapInStoredMatches(WikiString pageContent, bool forceNestedDecode);

        /// <summary>
        /// Replaces soft and hard line break markers in the specified page content with the provided override value or
        /// a default replacement.
        /// </summary>
        /// <remarks>This method modifies the provided WikiString instance in place. If different
        /// replacements are needed for soft and hard breaks, call this method separately for each type.</remarks>
        /// <param name="pageContent">The wiki page content in which line break markers will be replaced. Cannot be null.</param>
        /// <param name="overrideValue">The string to use as a replacement for both soft and hard line break markers. If null, uses "\r\n" for soft
        /// breaks and "<br />" for hard breaks.</param>
        public void SwapInLineBreaks(WikiString pageContent, string? overrideValue = null);
    }
}
