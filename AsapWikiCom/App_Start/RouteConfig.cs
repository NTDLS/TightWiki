using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AsapWikiCom
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Static/");

            /*
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Wiki", action = "Show", id = UrlParameter.Optional }
            );
            */

            routes.MapRoute(
                name: "DefaultWiki",
                url: "{navigation}",
                defaults: new { navigation = "Home", controller = "Wiki", action = "Show" }
            );

            routes.MapRoute(
                name: "DefaultOther",
                url: "{controller}/{action}",
                defaults: new { controller = "Wiki", action = "Login" }
            );

            routes.MapRoute(
                name: "CategoryPage",
                url: "{controller}/{action}/{navigation}",
                defaults: new { controller = "Wiki", action = "Show" }
            );
        }
    }
}
