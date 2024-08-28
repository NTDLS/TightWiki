using System.Text;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki.Engine.Implementation.Utility
{
    public class SearchCloud
    {
        public static string Build(List<string> searchTokens, int? maxCount = null)
        {
            var pages = PageRepository.PageSearch(searchTokens).OrderByDescending(o => o.Score).ToList();

            if (maxCount > 0)
            {
                pages = pages.Take((int)maxCount).ToList();
            }

            int pageCount = pages.Count;
            int fontSize = 7;
            int sizeStep = (pageCount > fontSize ? pageCount : (fontSize * 2)) / fontSize;
            int pageIndex = 0;

            var pageList = new List<TagCloudItem>();

            foreach (var page in pages)
            {
                pageList.Add(new TagCloudItem(page.Name, pageIndex, "<font size=\"" + fontSize + "\"><a href=\"/" + page.Navigation + "\">" + page.Name + "</a></font>"));

                if ((pageIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                pageIndex++;
            }

            var cloudHtml = new StringBuilder();

            pageList.Sort(TagCloudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCloudItem tag in pageList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }
    }
}
