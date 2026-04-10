namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a processing instruction applied to a wiki page,
    /// controlling how the page is rendered, cached, protected, or otherwise handled by the engine.
    /// </summary>
    public partial class TwProcessingInstruction
    {
        /// <summary>
        /// The unique identifier of the page this processing instruction is applied to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The processing instruction value, corresponding to a constant defined in <see cref="TwConstants.TwInstruction"/>,
        /// such as "Protect", "NoCache", or "Draft".
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
    }
}