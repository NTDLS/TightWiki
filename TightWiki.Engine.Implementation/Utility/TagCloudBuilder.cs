using System.Text;
using TightWiki.Library;
using TightWiki.Repository;

namespace TightWiki.Engine.Implementation.Utility
{
    public static class TagCloudBuilder
    {
        public static async Task<string> Build(string basePath, string seedTag, int? maxCount)
        {
            var tags = (await PageRepository.GetAssociatedTags(seedTag))
                .OrderByDescending(o => o.PageCount)
                .ToList();

            if (maxCount > 0)
            {
                tags = tags.Take(maxCount.Value).ToList();
            }

            if (tags.Count == 0)
            {
                return string.Empty;
            }

            int maxPageCount = tags.Max(o => o.PageCount);
            int minPageCount = tags.Min(o => o.PageCount);

            tags = tags.OrderBy(o => o.Tag).ToList();

            var html = new StringBuilder();

            html.Append("<div class=\"text-center lh-sm\">");

            foreach (var tag in tags)
            {
                var encodedTag = System.Net.WebUtility.HtmlEncode(tag.Tag);
                var url = $"{basePath}/Tags/Browse/{NamespaceNavigation.CleanAndValidate(tag.Tag)}";

                string tierClass = GetTierClass(tag.PageCount, minPageCount, maxPageCount);

                html.Append(
                    $"<a href=\"{url}\" class=\"badge rounded-pill  border text-decoration-none text-reset me-1 mb-1 {tierClass}\">{encodedTag}</a>");
            }

            html.Append("</div>");

            return html.ToString();
        }

        private static string GetTierClass(int pageCount, int minPageCount, int maxPageCount)
        {
            if (maxPageCount <= minPageCount)
            {
                return "fs-6";
            }

            double normalized = (double)(pageCount - minPageCount) / (maxPageCount - minPageCount);

            return normalized switch
            {
                >= 0.85 => "fs-4 fw-semibold px-3 py-2",
                >= 0.65 => "fs-5 fw-semibold px-3 py-2",
                >= 0.45 => "fs-6 fw-semibold px-2 py-1",
                >= 0.25 => "fs-6 px-2 py-1",
                _ => "small px-2 py-1"
            };
        }
    }
}