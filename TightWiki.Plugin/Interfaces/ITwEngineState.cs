using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Models;

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

        /// <summary>
        /// The localization text provider used to retrieve locale-specific strings during processing.
        /// </summary>
        ITwSharedLocalizationText Localizer { get; }

        /// <summary>
        /// The current user session state, or null if there is no active session.
        /// </summary>
        ITwSessionState? Session { get; }

        /// <summary>
        /// The HTTP query string parameters associated with the current page request.
        /// </summary>
        IQueryCollection QueryString { get; }

        /// <summary>
        /// The wiki engine instance responsible for processing the current page.
        /// </summary>
        ITwEngine Engine { get; }

        /// <summary>
        /// The wiki page currently being processed.
        /// </summary>
        ITwPage Page { get; }

        /// <summary>
        /// The specific revision of the page being processed, or null for the latest revision.
        /// </summary>
        int? Revision { get; }

        /// <summary>
        /// The set of match types that should be skipped during processing.
        /// </summary>
        public HashSet<TwMatchType> OmitMatches { get; }

        /// <summary>
        /// The current recursion depth, used to prevent infinite loops during nested page processing.
        /// </summary>
        public int NestDepth { get; }

        #endregion

        /// <summary>
        /// Indicates whether the current processing session is running in lite mode, a subset of full
        /// wiki engine functionality designed for comments and other contexts where performance is critical
        /// and certain features are not needed or desired.
        /// </summary>
        bool IsLite { get; }

        /// <summary>
        /// Logger instance for the current engine state, allowing plugins and custom functions
        /// to log information, warnings, and errors during wiki processing.
        /// </summary>
        public ILogger<ITwEngine> Logger { get; }

        #region State.

        /// <summary>
        /// The custom page title set by a call to @@Title("...") during processing,
        /// or null if no custom title was specified.
        /// </summary>
        public string? PageTitle { get; set; }

        /// <summary>
        /// Variables defined during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        Dictionary<string, string> Variables { get; }

        /// <summary>
        /// Snippets defined during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        Dictionary<string, string> Snippets { get; }

        /// <summary>
        /// Tags defined during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        List<string> Tags { get; set; }

        /// <summary>
        /// Processing instructions defined during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        List<string> ProcessingInstructions { get; }

        /// <summary>
        /// Outgoing page links identified during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        List<TwPageReference> OutgoingLinks { get; }

        /// <summary>
        /// Table of contents entries collected during wiki processing, either through plugin handlers or standard functions.
        /// </summary>
        List<TwTableOfContentsTag> TableOfContents { get; }

        /// <summary>
        /// A list of header messages such as page alerts to be displayed in the final output,
        /// collected during wiki processing either through plugin handlers or standard functions.
        /// </summary>
        List<string> Headers { get; }

        #endregion

        #region Results.

        /// <summary>
        /// The final rendered HTML output produced by the wiki processing session.
        /// </summary>
        string HtmlResult { get; }

        /// <summary>
        /// The total time taken to process the wiki page.
        /// </summary>
        TimeSpan ProcessingTime { get; }

        /// <summary>
        /// The total number of errors encountered during wiki processing.
        /// </summary>
        int ErrorCount { get; }

        /// <summary>
        /// The total number of markup matches found during wiki processing.
        /// </summary>
        int MatchCount { get; }

        #endregion

        /// <summary>
        /// The HTML tag name used to identify anything that needs a name. Use in concuntion with the GetNextStepNumber()
        /// method to generate unique identifiers for tags during processing.
        /// </summary>
        string TagMarker { get; }

        /// <summary>
        /// Gets the next string to use for generating unique tag identifiers during processing, incrementing the internal counter to ensure uniqueness.
        /// </summary>
        /// <param name="prefix">String to be prepended to the result</param>
        public string GetNextTagMarker(string prefix);

        /// <summary>
        /// Stores a typed value in the engine state for the duration of the current wiki processing session.
        /// </summary>
        public void SetStateValue<T>(string key, T value);

        /// <summary>
        /// Attempts to retrieve a typed value from the engine state by key.
        /// Returns true if the value was found, false otherwise.
        /// </summary>
        public bool TryGetStateValue<T>(string key, [MaybeNullWhen(false)] out T? outValue);

        /// <summary>
        /// Retrieves a typed value from the engine state by key, returning the specified default value if the key is not found.
        /// </summary>
        public T GetStateValue<T>(string key, T defaultValue);

        /// <summary>
        /// Generates a unique token safe for use in HTTP query parameters, avoiding collisions during processing.
        /// </summary>
        string GetNextHttpQueryToken();

        /// <summary>
        /// Transforms an included wiki page, processing any wiki markup injected by a function separately from the parent page.
        /// </summary>
        /// <param name="page">The child page to process.</param>
        /// <param name="revision">The optional revision of the child page to process.</param>
        Task<ITwEngineState> TransformChild(ITwPage page, int? revision = null);

        /// <summary>
        /// Replaces placeholders in the specified page content with previously stored match values.
        /// </summary>
        /// <param name="pageContent">The page content in which placeholders will be replaced. Cannot be null.</param>
        /// <param name="forceNestedDecode">If true, matches are replaced even if they are set to not allow nested decode.
        /// Force decode is typically only executed at the end of all processing but is made available here for special use cases by custom functions.</param>
        public void SwapInStoredMatches(TwString pageContent, bool forceNestedDecode);

        /// <summary>
        /// Replaces soft and hard line break markers in the specified page content with the provided override value or
        /// a default replacement.
        /// </summary>
        /// <remarks>This method modifies the provided WikiString instance in place. If different
        /// replacements are needed for soft and hard breaks, call this method separately for each type.</remarks>
        /// <param name="pageContent">The wiki page content in which line break markers will be replaced. Cannot be null.</param>
        /// <param name="overrideValue">The string to use as a replacement for both soft and hard line break markers. If null, uses "\r\n" for soft
        /// breaks and "&lt;br /&gt;" for hard breaks.</param>
        public void SwapInLineBreaks(TwString pageContent, string? overrideValue = null);
    }
}