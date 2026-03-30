using System.Text;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Engine
{
    public class TwSearchCloudBuilder
    {
        public static async Task<string> Build(ITwPageRepository pageRepository, string basePath, List<string> searchTokens, int? maxCount = null)
        {
            var pages = (await pageRepository.PageSearch(searchTokens))
                .OrderByDescending(o => o.Score)
                .ToList();

            if (maxCount > 0)
            {
                pages = pages.Take((int)maxCount).ToList();
            }

            int pageCount = pages.Count;
            int fontSize = 7;
            int sizeStep = (pageCount > fontSize ? pageCount : (fontSize * 2)) / fontSize;
            int pageIndex = 0;

            var pageList = new List<TwTagCloudItem>();

            foreach (var page in pages)
            {
                pageList.Add(new TwTagCloudItem(page.Name, pageIndex, "<font size=\"" + fontSize + $"\"><a href=\"{basePath}/" + page.Navigation + "\">" + page.Name + "</a></font>"));

                if ((pageIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                pageIndex++;
            }

            var cloudHtml = new StringBuilder();

            pageList.Sort(TwTagCloudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TwTagCloudItem tag in pageList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }
    }
}
