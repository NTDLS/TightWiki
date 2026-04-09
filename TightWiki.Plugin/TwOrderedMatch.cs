namespace TightWiki.Plugin
{
    /// <summary>
    /// Represents a match found in the input string during regex processing, containing the matched value and its index in the original string.
    /// </summary>
    public class TwOrderedMatch
    {
        /// <summary>
        /// Gets or sets the string value associated with this match.
        /// </summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the index of the match in the original string.
        /// </summary>
        public int Index { get; set; }
    }
}
