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

            #region File.

            routes.MapRoute(
                name: "File_Binary",
                url: "File/Binary/{navigation}",
                defaults: new { controller = "File", action = "Binary", navigation = "" }
            );

            routes.MapRoute(
                name: "File_Image",
                url: "File/Image/{navigation}",
                defaults: new { controller = "File", action = "Image", navigation = "" }
            );

            routes.MapRoute(
                name: "File_Png",
                url: "File/Png/{navigation}",
                defaults: new { controller = "File", action = "Png", navigation = "" }
            );

            routes.MapRoute(
                name: "File_Upload",
                url: "File/Upload/{navigation}",
                defaults: new { controller = "File", action = "Upload", navigation = "" }
            );

            #endregion

            routes.MapRoute(
                name: "Tag_Associations",
                url: "Tag/Browse/{navigation}",
                defaults: new { controller = "Tags", action = "Browse", navigation = "Home" }
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
