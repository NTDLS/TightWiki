namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public interface IHeadingHandler
    {
        /// <summary>
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        public HandlerResult Handle(ITightEngineState state, int depth, string link, string text);
    }
}
