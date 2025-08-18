using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;

namespace TightWiki.Extensions
{
    public static class IViewLocalizerExtensions
    {
        public static IHtmlContent Format(this IViewLocalizer viewLocalizer, string key, params object?[] param)
            => new HtmlContentBuilder().AppendHtml(string.Format(viewLocalizer[key].Value, param));

        public static string Format(this LocalizedHtmlString localizedHtmlString, params object?[] param)
            => string.Format(localizedHtmlString.Value, param);
    }
}
