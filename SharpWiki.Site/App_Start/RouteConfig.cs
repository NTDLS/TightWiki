using System.Web.Mvc;
using System.Web.Routing;

namespace SharpWiki.Site
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
                defaults: new { controller = "Wiki", action = "Display", id = UrlParameter.Optional }
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


            routes.MapRoute(
                name: "File_Delete",
                url: "File/Delete/{navigation}",
                defaults: new { controller = "File", action = "Delete", navigation = "" }
            );

            #endregion

            routes.MapRoute(
                name: "File_Associations",
                url: "File/{action}/{navigation}",
                defaults: new { controller = "Tags", action = "Binary", navigation = "" }
            );

            routes.MapRoute(
                name: "File_Associations_revision",
                url: "File/{action}/{navigation}/r/{revision}",
                defaults: new { controller = "File", action = "Binary", navigation = "", revision = 1 }
            );

            routes.MapRoute(
                name: "Tag_Associations",
                url: "Tag/Browse/{navigation}",
                defaults: new { controller = "Tags", action = "Browse", navigation = "Home" }
            );

            routes.MapRoute(
                name: "Wiki",
                url: "Wiki/{action}/{navigation}",
                defaults: new { controller = "Wiki", action = "Display", navigation = "Home" }
            );

            routes.MapRoute(
                name: "DefaultWiki",
                url: "{navigation}",
                defaults: new { navigation = "Home", controller = "Wiki", action = "Display", revision = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "DefaultWiki_Revision",
                url: "{navigation}/r/{revision}",
                defaults: new { navigation = "Home", controller = "Wiki", action = "Display", revision = 1 }
            );

            routes.MapRoute(
                name: "User_Avatar",
                url: "User/Avatar/{UserAccountName}",
                defaults: new { controller = "User", action = "Avatar" }
            );

            routes.MapRoute(
                name: "Admin_Config",
                url: "Admin/Config",
                defaults: new { controller = "Admin", action = "Config" }
            );

            routes.MapRoute(
                name: "DefaultOther",
                url: "{controller}/{action}",
                defaults: new { controller = "Wiki", action = "Login" }
            );
        }
    }
}
