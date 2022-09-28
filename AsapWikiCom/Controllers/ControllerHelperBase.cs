using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Repository;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace AsapWikiCom.Controllers
{
    public class ControllerHelperBase : Controller
    {
        public StateContext context = new StateContext();

        public byte[] ConvertToBytes(HttpPostedFileBase image)
        {
            BinaryReader reader = new BinaryReader(image.InputStream);
            byte[] imageBytes = reader.ReadBytes((int)image.ContentLength);
            return imageBytes;
        }

        public void Configure()
        {
            HydrateSecurityContext();

            var config = ConfigurationEntryRepository.GetConfigurationEntryValuesByGroupName("Basic");
            ViewBag.Title = ViewBag.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.BrandImageSmall = ViewBag.Copyright = config.Where(o => o.Name == "Brand Image (Small)").FirstOrDefault()?.Value;
            ViewBag.Name = config.Where(o => o.Name == "Name").FirstOrDefault()?.Value;
            ViewBag.Title = ViewBag.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.FooterBlurb = config.Where(o => o.Name == "FooterBlurb").FirstOrDefault()?.Value;
            ViewBag.Copyright = config.Where(o => o.Name == "Copyright").FirstOrDefault()?.Value;
            //ViewBag.PageUri = $"{RouteData.Values["controller"]}/{RouteData.Values["action"]}/{RouteData.Values["navigation"]}";
            ViewBag.Navigation = RouteValue("navigation");
        }

        public void HydrateSecurityContext()
        {
            context.IsAuthenticated = false;

            if (User.Identity.IsAuthenticated)
            {
                var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                var decodedTicket = FormsAuthentication.Decrypt(cookie.Value);
                var roles = decodedTicket.UserData.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                var principal = new GenericPrincipal(User.Identity, roles);

                context.IsAuthenticated = User.Identity.IsAuthenticated;
                if (context.IsAuthenticated)
                {
                    context.Roles = roles.ToList();
                    context.User = UserRepository.GetUserById(int.Parse(principal.Identity.Name));
                }
            }
        }

        public bool PerformLogin(string emailAddress, string password)
        {
            var user = UserRepository.GetUserByEmailAndPassword(emailAddress, password);
            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(user.Id.ToString(), false);

                var roles = RoleRepository.GetUserRolesByUserId(user.Id);
                string arrayOfRoles = string.Join("|", roles.Select(o => o.Name));

                var ticket = new FormsAuthenticationTicket(
                     version: 1,
                     name: user.Id.ToString(),
                     issueDate: DateTime.Now,
                     expiration: DateTime.Now.AddMinutes(Session.Timeout),
                     isPersistent: false,
                     userData: String.Join("|", arrayOfRoles));

                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

                Response.Cookies.Add(cookie);

                return true;
            }

            return false;
        }

        public string RouteValue(string key, string defaultValue = "")
        {
            if (RouteData.Values.ContainsKey(key))
            {
                return RouteData.GetRequiredString(key);
            }
            return defaultValue;
        }
    }
}
