using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AsapWiki.Shared.Models
{
    public class EditPage
    {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Navigation { get; set; }
		public string Description { get; set; }
		[AllowHtml]
		public string Body { get; set; }
	}
}
