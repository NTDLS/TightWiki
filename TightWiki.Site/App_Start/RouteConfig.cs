using System.Web.Mvc;
using System.Web.Routing;

namespace TightWiki.Site
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Static/");

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
                url: "{pageNavigation}/History/{page}",
                defaults: new { pageNavigation = "Home", controller = "Page", action = "History", pageRevision = UrlParameter.Optional, page = 1 }
            );

            routes.MapRoute(
                name: "Page_Revert_History",
                url: "{pageNavigation}/Revert/{pageRevision}",
                defaults: new { pageNavigation = "Home", controller = "Page", action = "Revert" }
            );

            routes.MapRoute(
                name: "Page_Search",
                url: "Page/Search/{page}",
                defaults: new { controller = "Page", action = "Search", page = 1 }
            );

            routes.MapRoute(
                name: "Admin_Moderate",
                url: "Admin/Moderate/{page}",
                defaults: new { controller = "Admin", action = "Moderate", page = 1 }
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

            routes.MapRoute(
                name: "Tag_Associations",
                url: "Tag/Browse/{pageNavigation}",
                defaults: new { controller = "Tags", action = "Browse", pageNavigation = "Home" }
            );

            routes.MapRoute(
                name: "User_Avatar",
                url: "User/{userAccountName}/Avatar",
                defaults: new { controller = "User", action = "Avatar" }
            );

            routes.MapRoute(
                name: "User_Confirm",
                url: "User/{userAccountName}/Confirm/{verificationCode}",
                defaults: new { controller = "User", action = "Confirm" }
            );
            
            routes.MapRoute(
                name: "User_Reset",
                url: "User/{userAccountName}/Reset/{verificationCode}",
                defaults: new { controller = "User", action = "Reset" }
            );

            routes.MapRoute(
                name: "Admin_Account",
                url: "Admin/Account/{navigation}",
                defaults: new { controller = "Admin", action = "Account", navigation = "admin" }
            );

            routes.MapRoute(
                name: "Admin_Generic",
                url: "Admin/{action}/{page}",
                defaults: new { controller = "Admin", action = "Config", page = 1 }
            );

            routes.MapRoute(
                name: "DefaultOther",
                url: "{controller}/{action}",
                defaults: new { controller = "Page", action = "Login" }
            );

        }
    }
}