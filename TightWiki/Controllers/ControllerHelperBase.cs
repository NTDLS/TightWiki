using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using TightWiki.Shared;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using static TightWiki.Shared.Wiki.Constants;

namespace TightWiki.Controllers
{
    public class ControllerHelperBase : Controller
    {
        public StateContext context = new StateContext();

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Configure();
        }

        public void Configure()
        {
            HydrateSecurityContext();

            context.PathAndQuery = Request.GetEncodedPathAndQuery();
            context.PageNavigation = RouteValue("pageNavigation");
            context.PageRevision = RouteValue("pageRevision");
            context.Title = Global.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.Context = context;
        }

        public void HydrateSecurityContext()
        {
            context.IsAuthenticated = false;

            if (User.Identity.IsAuthenticated)
            {
                try
                {
                    int? userId = null;

                    if (User.Identity.AuthenticationType == "Google")
                    {
                        var result = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
                        var firstIdentity = result.Principal.Identities.FirstOrDefault();
                        var emailddress = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.Email)?.FirstOrDefault().Value;
                        var lastName = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.Surname)?.FirstOrDefault().Value;
                        var firstName = firstIdentity.Claims.Where(o => o.Type == ClaimTypes.GivenName)?.FirstOrDefault().Value;
                        context.IsPartiallyAuthenticated = true;

                        var user = UserRepository.GetUserByEmail(emailddress);
                        if (user != null)
                        {
                            firstIdentity.AddClaim(new Claim(ClaimTypes.Name, user.AccountName));
                            firstIdentity.AddClaim(new Claim(ClaimTypes.Sid, user.Id.ToString()));
                            firstIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
                            userId = user.Id;
                        }
                    }
                    else
                    {
                        userId = int.Parse(User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid).Value);
                    }

                    var role = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value?.ToString();
                    if (userId != null && role != null)
                    {
                        context.IsAuthenticated = User.Identity.IsAuthenticated;
                        context.IsPartiallyAuthenticated = false;
                        if (context.IsAuthenticated)
                        {
                            context.Role = role;
                            context.User = UserRepository.GetUserById((int)userId);
                        }
                    }
                }
                catch
                {
                    HttpContext.SignOutAsync();
                    HttpContext.SignOutAsync(User.Identity.AuthenticationType);
                    //throw;
                }
            }
        }

        public int SavePage(Page page)
        {
            bool alreadyExisted = (page.Id != 0);

            page.Id = PageRepository.SavePage(page);

            var wikifier = new Wikifier(context, page, null, Request.Query, new WikiMatchType[] { WikiMatchType.Function });
            PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
            var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
            PageRepository.SavePageTokens(pageTokens);

            if (alreadyExisted)
            {
                PageRepository.UpdatePageReferences(page.Id, wikifier.OutgoingLinks);
            }
            else
            {
                //This will update the pageid of referenes that have been saved to the navigation link.
                PageRepository.UpdateSinglePageReference(page.Navigation);
            }

            //Debug.WriteLine($"Name {page.Name}, Matches: {wikifier.MatchCount}, Errors:{wikifier.ErrorCount}, Duration: {wikifier.ProcessingTime.TotalMilliseconds}");

            Cache.ClearClass($"Page:{page.Navigation}");

            return page.Id;
        }

        public void PerformLogin(string accountNameOrEmail, string password, bool isPasswordHash, bool persist = false)
        {
            var requireEmailVerification = ConfigurationRepository.Get<bool>("Membership", "Require Email Verification");

            User user;

            if (isPasswordHash)
            {
                user = UserRepository.GetUserByAccountNameOrEmailAndPasswordHash(accountNameOrEmail, password);
            }
            else
            {
                user = UserRepository.GetUserByAccountNameOrEmailAndPassword(accountNameOrEmail, password);
            }

            if (user != null)
            {
                if (requireEmailVerification == true && user.EmailVerified == false)
                {
                    throw new Exception("Email address has not been verified. Check your email or use the password reset link to confirm your email address.");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.EmailAddress),
                    new Claim(ClaimTypes.Name, user.AccountName),
                    new Claim(ClaimTypes.Sid, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = persist, // Set the authentication cookie to be persistent
                };

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                UserRepository.UpdateUserLastLoginDateByUserId(user.Id);
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
                return RouteData.Values[key]?.ToString();
            }
            return defaultValue;
        }
    }
}
