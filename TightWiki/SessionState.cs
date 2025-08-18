using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Caching;
using TightWiki.Exceptions;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki
{
    public class SessionState : ISessionState
    {
        public IQueryCollection? QueryString { get; set; }

        #region Authentication.

        public bool IsAuthenticated { get; set; }
        public IAccountProfile? Profile { get; set; }
        public bool IsAdministrator { get; set; }
        public Theme UserTheme { get; set; } = new();
        public List<ApparentAccountPermission> Permissions { get; set; } = new();

        #endregion

        #region Current Page.

        /// <summary>
        /// Custom page title set by a call to @@Title("...")
        /// </summary>
        public string? PageTitle { get; set; }
        public bool ShouldCreatePage { get; set; }
        public string PageNavigation { get; set; } = string.Empty;
        public string PageNavigationEscaped { get; set; } = string.Empty;
        public string PageTags { get; set; } = string.Empty;
        public ProcessingInstructionCollection PageInstructions { get; set; } = new();

        /// <summary>
        /// The "page" here is more of a "mock page", we use the name for various stuff.
        /// </summary>
        public IPage Page { get; set; } = new Models.DataModels.Page() { Name = GlobalConfiguration.Name };

        #endregion

        public SessionState Hydrate(SignInManager<IdentityUser> signInManager, PageModel pageModel)
        {
            QueryString = pageModel.Request.Query;

            HydrateSecurityContext(pageModel.HttpContext, signInManager, pageModel.User);
            return this;
        }

        public SessionState Hydrate(SignInManager<IdentityUser> signInManager, Controller controller)
        {
            QueryString = controller.Request.Query;
            PageNavigation = RouteValue("givenCanonical", "Home");
            PageNavigationEscaped = Uri.EscapeDataString(PageNavigation);

            HydrateSecurityContext(controller.HttpContext, signInManager, controller.User);

            string RouteValue(string key, string defaultValue = "")
            {
                if (controller.RouteData.Values.ContainsKey(key))
                {
                    return controller.RouteData.Values[key]?.ToString() ?? defaultValue;
                }
                return defaultValue;
            }

            return this;
        }

        private void HydrateSecurityContext(HttpContext httpContext, SignInManager<IdentityUser> signInManager, ClaimsPrincipal user)
        {
            IsAuthenticated = false;

            UserTheme = GlobalConfiguration.SystemTheme;

            if (signInManager.IsSignedIn(user))
            {
                try
                {
                    string emailAddress = (user.Claims.First(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var userId = Guid.Parse((user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        if (UsersRepository.TryGetBasicProfileByUserId(userId, out var profile))
                        {
                            Profile = profile;
                            IsAdministrator = UsersRepository.IsUserMemberOfAdministrators(userId);
                            Permissions = UsersRepository.GetApparentAccountPermissions(userId).ToList();
                            UserTheme = ConfigurationRepository.GetAllThemes().SingleOrDefault(o => o.Name == Profile.Theme) ?? GlobalConfiguration.SystemTheme;
                            IsAuthenticated = true;
                        }
                        else
                        {
                            //User is signed in, but does not have a profile.
                            //This likely means that the user has authenticated externally, but has yet to complete the signup process.
                        }
                    }
                }
                catch (Exception ex)
                {
                    httpContext.SignOutAsync();
                    if (user.Identity != null)
                    {
                        httpContext.SignOutAsync(user.Identity.AuthenticationType);
                    }

                    ExceptionRepository.InsertException(ex);
                }
            }
        }

        /// <summary>
        /// Sets the current context pageId and optionally the revision.
        /// </summary>
        public void SetPageId(int? pageId, int? revision = null)
        {
            Page = new Models.DataModels.Page();
            PageInstructions = new();
            PageTags = string.Empty;

            if (pageId != null)
            {
                Page = PageRepository.GetLimitedPageInfoByIdAndRevision((int)pageId, revision)
                    ?? throw new Exception("Page not found");

                PageInstructions = PageRepository.GetPageProcessingInstructionsByPageId(Page.Id);

                if (GlobalConfiguration.IncludeWikiTagsInMeta)
                {
                    PageTags = string.Join(",", PageRepository.GetPageTagsById(Page.Id)?.Select(o => o.Tag) ?? []);
                }
            }
        }

        #region Permissions.

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public bool HoldsPermission(Permission[] permissions)
            => HoldsPermission(Page.Navigation, permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public bool HoldsPermission(Permission permission)
            => HoldsPermission(Page.Navigation, permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public bool HoldsPermission(string? givenCanonical, Permission permission)
            => HoldsPermission(givenCanonical, [permission]);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public bool HoldsPermission(string? givenCanonical, Permission[] permissions)
        {
            if (IsAdministrator)
            {
                return true;
            }

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [givenCanonical, string.Join("|", permissions).ToLowerInvariant()]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                Models.DataModels.Page? page = null;

                if (givenCanonical != null)
                {
                    var navigation = new NamespaceNavigation(givenCanonical);
                    page = PageRepository.GetPageInfoByNavigation(navigation.Canonical);
                }

                string denyString = PermissionDisposition.Deny.ToString();
                string allowString = PermissionDisposition.Allow.ToString();

                bool holdsPermission = false;

                foreach (var permission in permissions)
                {
                    if (holdsPermission)
                    {
                        break; // If we already hold the permission, no need to check further.
                    }

                    string permissionString = permission.ToString();

                    if (page != null)
                    {
                        var pageIdString = page.Id.ToString();

                        //Check to see the the user has been explicitly denied access to the current page.
                        if (Permissions.Any(o => o.PageId == pageIdString
                            && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                            && o.PermissionDisposition.Equals(denyString, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }
                        //Check to see the the user has been explicitly granted access to the current page.
                        if (Permissions.Any(o => o.PageId == pageIdString
                            && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                            && o.PermissionDisposition.Equals(allowString, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            holdsPermission = true;
                            continue;
                        }

                        //Check to see the the user has been explicitly denied access to the current namespace.
                        if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                            && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                            && o.PermissionDisposition.Equals(denyString, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            continue;
                        }

                        //Check to see the the user has been explicitly granted access to the current namespace.
                        if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                            && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                            && o.PermissionDisposition.Equals(allowString, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            holdsPermission = true;
                            continue;
                        }
                    }

                    //Check to see the the user has been explicitly denied access to all pages.
                    if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                        && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                        && o.PermissionDisposition.Equals(denyString, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    //Check to see the the user has been explicitly granted access to all pages.
                    if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                        && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                        && o.PermissionDisposition.Equals(allowString, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        holdsPermission = true;
                        continue;
                    }

                    //Check to see the the user has been explicitly denied access to all namespaces.
                    if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                        && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                        && o.PermissionDisposition.Equals(denyString, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }

                    //Check to see the the user has been explicitly granted access to all namespaces.
                    if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                        && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                        && o.PermissionDisposition.Equals(allowString, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        holdsPermission = true;
                        continue;
                    }
                }

                return holdsPermission;
            }, WikiCache.DefaultCacheSeconds);
        }

        //public bool IsMemberOf(string role, string[] roles)
        //    => roles.Contains(role);

        //TODO: do we need this?
        public void RequireAuthorizedPermission()
        {
            if (!IsAuthenticated) throw new UnauthorizedException();
        }

        /// <summary>
        /// Throws an exception if the user does not hold any of the given permission for given page.
        /// </summary>
        public void RequirePermission(string? givenCanonical, Permission[] permissions)
        {
            if (!HoldsPermission(givenCanonical, permissions))
            {
                throw new UnauthorizedException($"You do not have permission to perform the action: {string.Join(", ", permissions)}");
            }
        }

        /// <summary>
        /// Throws an exception if the user does not hold the given permission for given page.
        /// </summary>
        public void RequirePermission(string? givenCanonical, Permission permission)
        {
            if (!HoldsPermission(givenCanonical, permission))
            {
                throw new UnauthorizedException($"You do not have permission to perform the action: {permission}");
            }
        }

        /// <summary>
        /// Throws an exception if the user is not an administrator.
        /// </summary>
        public void RequireAdminPermission()
        {
            if (!IsAdministrator)
            {
                throw new UnauthorizedException($"You do not have administrative permissions");
            }
        }

        #endregion

        public DateTime LocalizeDateTime(DateTime datetime)
            => TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());

        public TimeZoneInfo GetPreferredTimeZone()
        {
            if (string.IsNullOrEmpty(Profile?.TimeZone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(GlobalConfiguration.DefaultTimeZone);
            }
            return TimeZoneInfo.FindSystemTimeZoneById(Profile.TimeZone);
        }
    }
}
