namespace TightWiki.Plugin.Models
{
    public partial class TwProcessingInstruction
    {
        public int PageId { get; set; }
        /// <summary>
        /// TightWiki.Library.Constants.WikiInstruction
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
    }
}
