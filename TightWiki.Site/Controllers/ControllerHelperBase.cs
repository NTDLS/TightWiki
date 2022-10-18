using TightWiki.Shared.Library;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace TightWiki.Site.Controllers
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

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");

            ViewBag.Config = new ViewBagConfig
            {
                Context = context,
                IncludeWikiDescriptionInMeta = functConfig.As<bool>("Include wiki Description in Meta"),
                IncludeWikiTagsInMeta = functConfig.As<bool>("Include wiki Tags in Meta"),
                HTMLHeader = htmlConfig.As<string>("Header"),
                HTMLFooter = htmlConfig.As<string>("Footer"),
                HTMLPreBody = htmlConfig.As<string>("Pre-Body"),
                HTMLPostBody = htmlConfig.As<string>("Post-Body"),
                BrandImageSmall = basicConfig.As<string>("Brand Image (Small)"),
                Name = basicConfig.As<string>("Name"),
                Title = basicConfig.As<string>("Name"), //Default the title to the name. This will be replaced when the page is found and loaded.
                FooterBlurb = basicConfig.As<string>("FooterBlurb"),
                Copyright = basicConfig.As<string>("Copyright"),
                PageNavigation = RouteValue("pageNavigation"),
                PageRevision = RouteValue("pageRevision"),
                AllowGuestsToViewHistory = basicConfig.As<bool>("Allow Guests to View History"),
                MenuItems = MenuItemRepository.GetAllMenuItems()
            };

            context.Config = ViewBag.Config;
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

        public int SavePage(Page page)
        {
            page.Id = PageRepository.SavePage(page);

            var wikifier = new Wikifier(context, page, null, Request.QueryString);
            PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
            var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
            PageRepository.SavePageTokens(pageTokens);

            Cache.ClearClass($"Page:{page.Navigation}");

            return page.Id;
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

        public void PerformLogin(string emailAddress, string password, bool isPasswordHash)
        {
            var requireEmailVerification = ConfigurationRepository.Get<bool>("Membership", "Require Email Verification");

            User user;

            if (isPasswordHash)
            {
                user = UserRepository.GetUserByEmailAndPasswordHash(emailAddress, password);
            }
            else
            {
                user = UserRepository.GetUserByEmailAndPassword(emailAddress, password);
            }

            if (user != null)
            {
                if (requireEmailVerification == true && user.EmailVerified == false)
                {
                    throw new Exception("Email address has not been verified. Check your email or use the password reset link to confirm your email address.");
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
