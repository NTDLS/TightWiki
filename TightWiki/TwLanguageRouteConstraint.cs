using NTDLS.Helpers;
using TightWiki.Plugin.Library;

namespace TightWiki
{
    /// <summary>
    /// TODO: This does not appear to be used.
    /// </summary>
    public class TwLanguageRouteConstraint(TwSupportedCultures cultures)
        : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!values.ContainsKey("lang"))
                return false;

            var lang = values["lang"].EnsureNotNull().ToString();
            return cultures.AllCultures.Any(x => x.Name == lang);
        }
    }
}
