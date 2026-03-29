namespace TightWiki.Plugin.Models
{
    public class TwProcessingInstructionCollection
    {
        public List<TwProcessingInstruction> Collection { get; set; } = new();

        /// <summary>
        /// Returns true if the collection contains the given processing instruction.
        /// </summary>
        /// <param name="wikiInstruction">Value from the class WikiInstruction, such as WikiInstruction.Protect.</param>
        /// <returns></returns>
        public bool Contains(string wikiInstruction)
            => Collection.Any(o => string.Equals(o.Instruction, wikiInstruction, StringComparison.InvariantCultureIgnoreCase));
    }
}
