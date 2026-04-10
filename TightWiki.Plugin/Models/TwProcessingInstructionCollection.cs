namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the complete set of processing instructions applied to a wiki page,
    /// providing a typed collection with convenience methods for instruction lookups.
    /// </summary>
    public class TwProcessingInstructionCollection
    {
        /// <summary>
        /// The list of processing instructions associated with the page.
        /// </summary>
        public List<TwProcessingInstruction> Collection { get; set; } = new();

        /// <summary>
        /// Returns true if the collection contains the specified processing instruction,
        /// using a case-insensitive comparison.
        /// </summary>
        /// <param name="wikiInstruction">The instruction value to search for, such as a constant from <see cref="TwConstants.TwInstruction"/>.</param>
        public bool Contains(string wikiInstruction)
            => Collection.Any(o => string.Equals(o.Instruction, wikiInstruction, StringComparison.InvariantCultureIgnoreCase));
    }
}