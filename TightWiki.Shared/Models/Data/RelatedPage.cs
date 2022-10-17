namespace TightWiki.Shared.Models.Data
{
    public partial class RelatedPage
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Navigation { get; set; }
		public string Description { get; set; }
		public int Matches { get; set; }
	}
}
