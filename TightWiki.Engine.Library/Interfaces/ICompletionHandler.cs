namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public interface ICompletionHandler
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing competes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public void Complete(ITightEngineState state);
    }
}
