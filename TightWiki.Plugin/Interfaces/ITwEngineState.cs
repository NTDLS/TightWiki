using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Models;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Interface representing the state of the wiki engine during the processing of a wiki page.
    /// This interface provides access to various parameters, state information, and results related
    /// to the current wiki processing session. It is designed to be used by plugins and custom functions
    /// to interact with the wiki engine and manipulate the processing flow as needed.
    /// </summary>
    public interface ITwEngineState
    {
        #region Parameters.
        ITwSharedLocalizationText Localizer { get; }
        ITwSessionState? Session { get; }
        IQueryCollection QueryString { get; }

        ITwEngine Engine { get; }
        ITwPage Page { get; }
        int? Revision { get; }
        public HashSet<TwMatchType> OmitMatches { get; }
        public int NestDepth { get; } //Used for recursion.

        #endregion

        /// <summary>
        /// Whether or not the current processing session is being executed in "lite mode", which is a
        /// subset of the full wiki engine functionality that is designed for comments and other contexts
        /// where performance is critical and certain features are not needed or desired.
        /// </summary>
        bool IsLite { get; }

        /// Logger instance for the current engine state. This allows plugins and custom functions to log information, warnings, and errors during wiki processing.
        public ILogger<ITwEngine> Logger { get; }

        #region State.

        /// Custom page title set by a call to @@Title("...")
        public string? PageTitle { get; set; }

        /// Variables defined during wiki processing, either through plugin handlers or standard functions.
        Dictionary<string, string> Variables { get; }

        /// Snippets defined during wiki processing, either through plugin handlers or standard functions.
        Dictionary<string, string> Snippets { get; }

        /// Tags defined during wiki processing, either through plugin handlers or standard functions.
        List<string> Tags { get; set; }
        /// Processing instructions defined during wiki processing, either through plugin handlers or standard functions.
        List<string> ProcessingInstructions { get; }
        /// Outgoing links defined during wiki processing, either through plugin handlers or standard functions.
        List<TwPageReference> OutgoingLinks { get; }
        /// Table of contents entries defined during wiki processing, either through plugin handlers or standard functions.
        List<TwTableOfContentsTag> TableOfContents { get; }
        /// A list of headers (like page alerts) that need to be displayed in the final output, defined during wiki processing either through plugin handlers or standard functions.
        List<string> Headers { get; }

        #endregion

        #region Results.

        /// The final HTML result of the wiki processing.
        string HtmlResult { get; }
        /// Gets the total time taken to process the operation.
        TimeSpan ProcessingTime { get; }
        /// Gets the total number of errors encountered.
        int ErrorCount { get; }
        /// Gets the number of matches found by the operation.
        int MatchCount { get; }

        #endregion

        /// Link tag to use for table of contents entries.
        string TocName { get; }

        /// Used to store values for handlers that needs to survive only a single wiki processing session.
        public void SetStateValue<T>(string key, T value);

        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        public bool TryGetStateValue<T>(string key, [MaybeNullWhen(false)] out T? outValue);

        /// Used to get values for handlers that needs to survive only a single wiki processing session.
        public T GetStateValue<T>(string key, T defaultValue);

        /// Generates a unique token that can be used for HTTP query parameters to avoid collisions during processing.
        string GetNextHttpQueryToken();

        /// <summary>
        /// Transforms "included" wiki pages, for example if a wiki function
        /// injected additional wiki markup, this 'could' be processed separately.
        /// </summary>
        /// <param name="page">The child page to process</param>
        /// <param name="revision">The optional revision of the child page to process</param>
        Task<ITwEngineState> TransformChild(ITwPage page, int? revision = null);

        /// <summary>
        /// Replaces placeholders in the specified page content with previously stored match values.
        /// </summary>
        /// <param name="pageContent">The page content in which placeholders will be replaced. Cannot be null.</param>
        /// <param name="forceNestedDecode">If true, matches are replaced even if they are set to not allow nested decode.
        /// ForceDecode is typically only executed at the end of all processing but is made available here for special use cases by custom functions.
        public void SwapInStoredMatches(TwString pageContent, bool forceNestedDecode);

        /// <summary>
        /// Replaces soft and hard line break markers in the specified page content with the provided override value or
        /// a default replacement.
        /// </summary>
        /// <remarks>This method modifies the provided WikiString instance in place. If different
        /// replacements are needed for soft and hard breaks, call this method separately for each type.</remarks>
        /// <param name="pageContent">The wiki page content in which line break markers will be replaced. Cannot be null.</param>
        /// <param name="overrideValue">The string to use as a replacement for both soft and hard line break markers. If null, uses "\r\n" for soft
        /// breaks and "<br />" for hard breaks.</param>
        public void SwapInLineBreaks(TwString pageContent, string? overrideValue = null);
    }
}
