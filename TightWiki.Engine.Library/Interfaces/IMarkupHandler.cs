namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles basic markup/style instructions like bole, italic, underline, etc.
    /// </summary>
    public interface IMarkupHandler
    {
        /// <summary>
        /// Handles basic markup instructions like bole, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        public HandlerResult Handle(ITightEngineState state, char sequence, string scopeBody);
    }
}
