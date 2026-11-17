namespace TightWiki.Library.Api
{
    /// <summary>
    /// The form in which the body of a page is returned.
    /// </summary>
    public enum ApiPageFormat
    {
        /// <summary>The wiki markup as it was saved.</summary>
        Markup,
        /// <summary>The markup transformed to HTML.</summary>
        Html,
        /// <summary>The transformed HTML with the tags removed.</summary>
        Text
    }
}
