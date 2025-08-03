using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;

namespace TightWiki.Extensions
{
    public static class IViewLocalizerExtensions
    {
        public static IHtmlContent Format(this IViewLocalizer viewLocalizer, string key, params object[] objs)
        {
            return new HtmlContentBuilder().AppendHtml(String.Format(viewLocalizer[key].Value, objs));
        }


        public static string Value(this LocalizedHtmlString localizedHtmlString, params object[] objs)
        {
            return String.Format(localizedHtmlString.Value, objs);
        }
    }
}
