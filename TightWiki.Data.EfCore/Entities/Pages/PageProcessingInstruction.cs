namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A processing instruction declared by a page's markup (e.g. a rendering directive) (Pages.
    /// PageProcessingInstruction).
    /// </summary>
    public class PageProcessingInstruction
    {
        /// <summary>
        /// The identifier of the page this instruction was found on. Part of the composite primary key together
        /// with <see cref="Instruction"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive processing instruction text. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public string Instruction { get; set; } = string.Empty;

        /// <summary>
        /// The page this instruction was found on.
        /// </summary>
        public Page Page { get; set; } = null!;
    }
}
