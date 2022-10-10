using SharpWiki.Shared.Library;
using SharpWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace SharpWiki.Shared.Models
{
    public class ViewBagConfig
    {
        public string BrandImageSmall { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string FooterBlurb { get; set; }
        public string Copyright { get; set; }
        public string PageNavigation { get; set; }
        public string PageRevision { get; set; }
        public List<MenuItem> MenuItems { get; set; }
        public StateContext Context { get; set; }
        public bool AllowGuestsToViewHistory { get; set; }
        public string HTMLHeader { get; set; }
        public string HTMLFooter { get; set; }
        public string HTMLPreBody { get; set; }
        public string HTMLPostBody { get; set; }
    }
}
