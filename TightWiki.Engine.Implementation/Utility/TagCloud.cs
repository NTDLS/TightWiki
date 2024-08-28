using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki.Engine.Implementation.Utility
{
    public static class TagCloud
    {
        public static string Build(string seedTag, int? maxCount)
        {
            var tags = PageRepository.GetAssociatedTags(seedTag).OrderByDescending(o => o.PageCount).ToList();

            if (maxCount > 0)
            {
                tags = tags.Take((int)maxCount).ToList();
            }

            int tagCount = tags.Count;
            int fontSize = 7;
            int sizeStep = (tagCount > fontSize ? tagCount : (fontSize * 2)) / fontSize;
            int tagIndex = 0;

            var tagList = new List<TagCloudItem>();

            foreach (var tag in tags)
            {
                tagList.Add(new TagCloudItem(tag.Tag, tagIndex, "<font size=\"" + fontSize + "\"><a href=\"/Tag/Browse/" + NamespaceNavigation.CleanAndValidate(tag.Tag) + "\">" + tag.Tag + "</a></font>"));

                if ((tagIndex % sizeStep) == 0)
                {
                    fontSize--;
                }

                tagIndex++;
            }

            var cloudHtml = new StringBuilder();

            tagList.Sort(TagCloudItem.CompareItem);

            cloudHtml.Append("<table align=\"center\" border=\"0\" width=\"100%\"><tr><td><p align=\"justify\">");

            foreach (TagCloudItem tag in tagList)
            {
                cloudHtml.Append(tag.HTML + "&nbsp; ");
            }

            cloudHtml.Append("</p></td></tr></table>");

            return cloudHtml.ToString();
        }
    }
}
