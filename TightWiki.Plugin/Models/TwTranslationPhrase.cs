namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// A single phrase and its translation into one language.
    /// </summary>
    public class TwTranslationPhrase
    {
        /// <summary>
        /// The English phrase, which is also the key used to look up the translation.
        /// </summary>
        public string English { get; set; } = string.Empty;

        /// <summary>
        /// The phrase translated to the requested language.
        /// </summary>
        public string Translation { get; set; } = string.Empty;
    }
}
