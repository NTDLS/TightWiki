using System.Collections.Generic;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models
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
        public string HTMLHeader { get; set; }
        public string HTMLFooter { get; set; }
        public string HTMLPreBody { get; set; }
        public string HTMLPostBody { get; set; }
        public bool IncludeWikiDescriptionInMeta { get; set; }
        public bool IncludeWikiTagsInMeta { get; set; }
        public bool IncludeSearchOnNavbar { get; set; }
        public string PageTags { get; set; }
        public string PageDescription { get; set; }
        public string PathAndQuery { get; set; }
        public string DefaultTimeZone { get; set; }
    }
}