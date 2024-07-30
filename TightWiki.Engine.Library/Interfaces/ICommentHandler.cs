namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public interface ICommentHandler
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public HandlerResult Handle(ITightEngineState state, string text);
    }
}
