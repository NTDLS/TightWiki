using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace SharpWiki.Shared.Models.View
{
    public class EditPageModel
    {
		public int Id { get; set; }

		[Required] 
		public string Name { get; set; }
		public string Navigation { get; set; }
		public string Description { get; set; }
		[AllowHtml]
		public string Body { get; set; }
	}
}
