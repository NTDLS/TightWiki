using SharpWiki.Shared.Library;
using SharpWiki.Shared.Repository;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace SharpWiki.Site.Controllers
{
    public class ControllerHelperBase : Controller
    {
        public StateContext context = new StateContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Configure();
        }

        public void Configure()
        {
            HydrateSecurityContext();

            var config = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            ViewBag.Title = ViewBag.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.BrandImageSmall = ViewBag.Copyright = config.Where(o => o.Name == "Brand Image (Small)").FirstOrDefault()?.Value;
            ViewBag.Name = config.Where(o => o.Name == "Name").FirstOrDefault()?.Value;
            ViewBag.Title = ViewBag.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.FooterBlurb = config.Where(o => o.Name == "FooterBlurb").FirstOrDefault()?.Value;
            ViewBag.Copyright = config.Where(o => o.Name == "Copyright").FirstOrDefault()?.Value;
            //ViewBag.PageUri = $"{RouteData.Values["controller"]}/{RouteData.Values["action"]}/{RouteData.Values["navigation"]}";
            ViewBag.PageNavigation = RouteValue("pageNavigation");
            ViewBag.PageRevision = RouteValue("pageRevision");
            ViewBag.MenuItems = MenuItemRepository.GetAllMenuItems();

            ViewBag.Context = context;
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
            else if (ConfigurationRepository.Get("Membership", "Allow Guest", false))
            {
                PerformGuestLogin();
            }
        }

        public bool PerformGuestLogin()
        {
            var guestAccount = ConfigurationRepository.Get<string>("Membership", "Guest Account");

            var user = UserRepository.GetUserByNavigation(guestAccount);
            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(user.Id.ToString(), false);

                var roles = UserRepository.GetUserRolesByUserId(user.Id);
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

                UserRepository.UpdateUserLastLoginDateByUserId(user.Id);

                Response.Cookies.Add(cookie);

                return true;
            }

            return false;
        }

        public void PerformLogin(string emailAddress, string password)
        {
            var requireEmailVerification = ConfigurationRepository.Get<bool>("Membership", "Require Email Verification");

            var user = UserRepository.GetUserByEmailAndPassword(emailAddress, password);
            if (user != null)
            {
                if (requireEmailVerification == true && user.EmailVerified == false)
                {
                    throw new Exception("Email address has not been verified. Check you email or use the password reset link to confirm your email address.");
                }

                FormsAuthentication.SetAuthCookie(user.Id.ToString(), false);

                var roles = UserRepository.GetUserRolesByUserId(user.Id);
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

                UserRepository.UpdateUserLastLoginDateByUserId(user.Id);

                Response.Cookies.Add(cookie);
            }
            else
            {
                throw new Exception("Invalid Username or Password");
            }
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
