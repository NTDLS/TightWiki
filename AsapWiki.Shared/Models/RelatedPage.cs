using System;
using System.Runtime.Serialization;

namespace AsapWiki.Shared.Models
{
	public partial class RelatedPage : BaseModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Navigation { get; set; }
		public string Description { get; set; }
		public int Matches { get; set; }
	}
}
