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
                defaults: new { controller = "Page", action = "Display", id = UrlParameter.Optional }
            );
            */

            /*
            routes.MapRoute(
                name: "File_Upload",
                url: "File/Upload/{pageNavigation}",
                defaults: new { controller = "File", action = "Upload", pageNavigation = "" }
            );

            routes.MapRoute(
                name: "File_Delete",
                url: "File/Delete/{pageNavigation}",
                defaults: new { controller = "File", action = "Delete", pageNavigation = "" }
            );

            */

            routes.MapRoute(
                name: "File_Attachment",
                url: "File/{action}/{pageNavigation}/{fileNavigation}",
                defaults: new { controller = "File", action = "Binary", pageNavigation = "", fileNavigation = "", pageRevision = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "File_Attachment_Revision",
                url: "File/{action}/{pageNavigation}/{fileNavigation}/r/{pageRevision}",
                defaults: new { controller = "File", action = "Binary", pageNavigation = "", fileNavigation = "", pageRevision = 1 }
            );

            routes.MapRoute(
                name: "Page_Display",
                url: "{pageNavigation}",
                defaults: new { pageNavigation = "Home", controller = "Page", action = "Display", pageRevision = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Page_Display_Revision",
                url: "{pageNavigation}/r/{pageRevision}",
                defaults: new { pageNavigation = "Home", controller = "Page", action = "Display", pageRevision = 1 }
            );

            routes.MapRoute(
                name: "Page_Display_History",
                url: "{pageNavigation}/History",
                defaults: new { pageNavigation = "Home", controller = "Page", action = "History", pageRevision = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Page_Edit",
                url: "Page/Edit/{pageNavigation}",
                defaults: new { controller = "Page", action = "Edit", pageNavigation = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Page_Default",
                url: "Page/{action}/{pageNavigation}",
                defaults: new { controller = "Page", action = "Display", pageNavigation = "Home" }
            );

            //http://localhost/File/Binary/pageNav/ImageNav/r/10
            //http://localhost/File/Binary/pageNav/ImageNav/r/10

            routes.MapRoute(
                name: "Tag_Associations",
                url: "Tag/Browse/{pageNavigation}",
                defaults: new { controller = "Tags", action = "Browse", pageNavigation = "Home" }
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
                defaults: new { controller = "Page", action = "Login" }
            );

        }
    }
}