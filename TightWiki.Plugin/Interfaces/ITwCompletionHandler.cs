namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Handles wiki completion events.
    /// </summary>
    public interface ITwCompletionHandler
    {
        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing competes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        public Task Complete(ITwEngineState state);
    }
}
