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
                defaults: new { controller = "Wiki", action = "Content", id = UrlParameter.Optional }
            );
            */

            routes.MapRoute(
                name: "Attachment",
                url: "Wiki/Attachment/{navigation}",
                defaults: new { controller = "Wiki", action = "Attachment", navigation = "Home" }
            );

            routes.MapRoute(
                name: "TagAssociations",
                url: "Tag/Associations/{navigation}",
                defaults: new { controller = "Tags", action = "AssociationCloud", navigation = "Home" }
            );

            routes.MapRoute(
                name: "Wiki",
                url: "Wiki/{action}/{navigation}",
                defaults: new { controller = "Wiki", action = "Content", navigation = "Home" }
            );

            routes.MapRoute(
                name: "DefaultWiki",
                url: "{navigation}",
                defaults: new { navigation = "Home", controller = "Wiki", action = "Content" }
            );

            routes.MapRoute(
                name: "DefaultOther",
                url: "{controller}/{action}",
                defaults: new { controller = "Wiki", action = "Login" }
            );
        }
    }
}
