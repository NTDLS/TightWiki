using Microsoft.AspNetCore.Mvc.Localization;

namespace TightWiki.Extensions
{
    public static class IViewLocalizerExtensions
    {
        public static string Format(this IViewLocalizer viewLocalizer, string key, params object[] objs)
        {
            return viewLocalizer.Format(key, objs);
        }

        public static string Value(this LocalizedHtmlString localizedHtmlString, params object[] objs)
        {
            return String.Format(localizedHtmlString.Value, objs);
        }
    }
}
