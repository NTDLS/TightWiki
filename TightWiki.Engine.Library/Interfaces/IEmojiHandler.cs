namespace TightWiki.Engine.Library.Interfaces
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public interface IEmojiHandler
    {
        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public HandlerResult Handle(ITightEngineState state, string key, int scale);
    }
}
