namespace TightWiki.Models.DataModels
{
    public class ProcessingInstructionCollection
    {
        public List<ProcessingInstruction> Collection { get; set; } = new();

        /// <summary>
        /// Returns true if the collection contains the given processing instruction.
        /// </summary>
        /// <param name="wikiInstruction">WikiInstruction.Protect</param>
        /// <returns></returns>
        public bool Contains(string wikiInstruction)
            => Collection.Any(o => string.Equals(o.Instruction, wikiInstruction, StringComparison.InvariantCultureIgnoreCase));
    }
}
