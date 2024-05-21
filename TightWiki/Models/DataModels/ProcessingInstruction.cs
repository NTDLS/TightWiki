namespace TightWiki.Models.DataModels
{
    public partial class ProcessingInstruction
    {
        public int PageId { get; set; }
        /// <summary>
        /// TightWiki.Library.Constants.WikiInstruction
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
    }
}
