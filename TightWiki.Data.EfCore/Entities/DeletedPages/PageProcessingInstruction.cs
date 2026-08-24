namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A processing instruction that belonged to a page before it was soft-deleted (DeletedPages.
    /// PageProcessingInstruction), moved here verbatim from Pages.PageProcessingInstruction.
    /// </summary>
    public class PageProcessingInstruction
    {
        /// <summary>
        /// The identifier of the deleted page this instruction was found on. Part of the composite primary key
        /// together with <see cref="Instruction"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive processing instruction text. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
    }
}
