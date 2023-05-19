namespace TightWiki.Shared.Models.Data
{
    public partial class ProcessingInstruction
    {
        public int PageId { get; set; }
        /// <summary>
        /// TightWiki.Shared.Library.Constants.WikiInstruction
        /// </summary>
        public string Instruction { get; set; }
    }
}
