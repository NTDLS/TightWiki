using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NTDLS.Helpers;

namespace TightWiki.Library
{
    /// <summary>
    /// TODO: This does not appear to be used.
    /// </summary>
    public class LanguageRouteConstraint(SupportedCultures cultures)
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
