using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Caching;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Exceptions;
using TightWiki.Extensions;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using TightWiki.Translations;

namespace TightWiki
{
    public class SessionState
        : ISessionState
    {
        private readonly string _denyString = WikiPermissionDisposition.Deny.ToString();
        private readonly string _allowString = WikiPermissionDisposition.Allow.ToString();

        public IQueryCollection? QueryString { get; set; }
        public ILogger<ITightEngine>? Logger { get; private set; }

        #region Authentication.

        public bool IsAuthenticated { get; set; }
        public IAccountProfile? Profile { get; set; }
        public bool IsAdministrator { get; set; }
        public Theme UserTheme { get; set; } = new();
        public List<ApparentPermission> Permissions { get; set; } = new();

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
        public IWikiPage Page { get; set; } = new Models.DataModels.WikiPage() { Name = GlobalConfiguration.Name };

        #endregion

        /// <summary>
        /// This method is used to hydrate the session state from PageModelBase.
        /// </summary>
        public async Task<SessionState> Hydrate(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager, PageModel pageModel)
        {
            Logger = logger;
            QueryString = pageModel.Request.Query;

            await HydrateSecurityContext(pageModel.HttpContext, signInManager, pageModel.User);
            return this;
        }

        /// <summary>
        /// This method is used to hydrate the session state from WikiControllerBase.
        /// </summary>
        public async Task<SessionState> Hydrate(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager, Controller controller)
        {
            Logger = logger;

            QueryString = controller.Request.Query;
            PageNavigation = RouteValue("givenCanonical", "Home");
            PageNavigationEscaped = Uri.EscapeDataString(PageNavigation);

            await HydrateSecurityContext(controller.HttpContext, signInManager, controller.User);

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

        private async Task HydrateSecurityContext(HttpContext httpContext, SignInManager<IdentityUser> signInManager, ClaimsPrincipal user)
        {
            IsAuthenticated = false;

            UserTheme = GlobalConfiguration.SystemTheme;

            if (signInManager.IsSignedIn(user))
            {
                try
                {
                    //string emailAddress = (user.Claims.First(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var userId = Guid.Parse((user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        var profile = await UsersRepository.GetBasicProfileByUserId(userId);
                        if (profile != null)
                        {
                            Profile = profile;
                            IsAdministrator = await UsersRepository.IsUserMemberOfAdministrators(userId);
                            Permissions = await UsersRepository.GetApparentAccountPermissions(userId);
                            UserTheme = (await ConfigurationRepository.GetAllThemes()).SingleOrDefault(o => o.Name == Profile.Theme) ?? GlobalConfiguration.SystemTheme;
                            IsAuthenticated = true;
                            return;
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
                    await httpContext.SignOutAsync();
                    if (user.Identity != null)
                    {
                        await httpContext.SignOutAsync(user.Identity.AuthenticationType);
                    }

                    Logger?.LogError(ex, "An error occurred while hydrating the security context.");
                }
            }

            Permissions = await UsersRepository.GetApparentRolePermissions(WikiRoles.Anonymous);
        }

        /// <summary>
        /// Sets the current context pageId and optionally the revision.
        /// </summary>
        public async Task SetPageId(int? pageId, int? revision = null)
        {
            Page = new Models.DataModels.WikiPage();
            PageInstructions = new();
            PageTags = string.Empty;

            if (pageId != null)
            {
                Page = await PageRepository.GetLimitedPageInfoByIdAndRevision((int)pageId, revision)
                    ?? throw new Exception("Page not found");

                PageInstructions = await PageRepository.GetPageProcessingInstructionsByPageId(Page.Id);

                if (GlobalConfiguration.IncludeWikiTagsInMeta)
                {
                    PageTags = string.Join(",", (await PageRepository.GetPageTagsById(Page.Id))
                        ?.Select(o => o.Tag) ?? []);
                }
            }
        }

        #region Permissions.

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public async Task<bool> HoldsPermission(WikiPermission[] permissions)
            => await HoldsPermission(Page.Navigation, permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public async Task<bool> HoldsPermission(WikiPermission permission)
            => await HoldsPermission(Page.Navigation, permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public async Task<bool> HoldsPermission(string? givenCanonical, WikiPermission permission)
            => await HoldsPermission(givenCanonical, [permission]);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public async Task<bool> HoldsPermission(string? givenCanonical, WikiPermission[] permissions)
        {
            if (IsAdministrator)
            {
                return true;
            }

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [givenCanonical, Profile?.UserId, string.Join("|", permissions).ToLowerInvariant()]);

            return await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                Models.DataModels.WikiPage? page = null;

                if (givenCanonical != null)
                {
                    var navigation = new NamespaceNavigation(givenCanonical);
                    page = await PageRepository.GetPageInfoByNavigation(navigation.Canonical);
                }

                foreach (var permission in permissions)
                {
                    //Remember that we are evaluating to see if the user holds ANY one of the supplied permissions.
                    //So, we are going to evaluate each permission in the supplied array individually,
                    //  ignoring any NULL results (as NULL means that the permission was not explicitly allowed or denied).
                    //If the permission is explicitly allowed, we return true.
                    //If the permission is explicitly denied, we move to the next permission because permission could
                    //  have been denied on a namespace but explicitly allowed on a page (and yes, we test in that order).
                    //Also note that we do not pass the page when the permission is Create - because that would make no sense.
                    if (EvaluatePermission(permission, permission == WikiPermission.Create ? null : page) == true)
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        private bool? EvaluatePermission(WikiPermission permission, Models.DataModels.WikiPage? page)
        {
            string permissionString = permission.ToString();

            if (page != null)
            {
                var pageIdString = page.Id.ToString();

                //Check to see the the user has been explicitly denied access to the current page.
                if (Permissions.Any(o => o.PageId == pageIdString
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return false;
                }
                //Check to see the the user has been explicitly granted access to the current page.
                if (Permissions.Any(o => o.PageId == pageIdString
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return true;
                }

                //Check to see the the user has been explicitly denied access to the current namespace.
                if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return false;
                }

                //Check to see the the user has been explicitly granted access to the current namespace.
                if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return true;
                }
            }

            //Check to see the the user has been explicitly denied access to all pages.
            if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            //Check to see the the user has been explicitly granted access to all pages.
            if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            //Check to see the the user has been explicitly denied access to all namespaces.
            if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            //Check to see the the user has been explicitly granted access to all namespaces.
            if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            return null;
        }

        public async Task RequireAuthorizedPermission()
        {
            if (!IsAuthenticated)
            {
                var localizer = LocalizerFactory.Create();
                throw new UnauthorizedException(localizer["You are not authorized"]);
            }
        }

        /// <summary>
        /// Throws an exception if the user does not hold any of the given permission for given page.
        /// </summary>
        public async Task RequirePermission(string? givenCanonical, WikiPermission[] permissions)
        {
            if (!await HoldsPermission(givenCanonical, permissions))
            {
                var localizer = LocalizerFactory.Create();
                throw new UnauthorizedException(localizer["You do not have permission to perform the action: {0}"]
                    .Format(string.Join(", ", permissions.Select(o => localizer[o.ToString()]))));
            }
        }

        /// <summary>
        /// Throws an exception if the user does not hold the given permission for given page.
        /// </summary>
        public async Task RequirePermission(string? givenCanonical, WikiPermission permission)
        {
            if (!await HoldsPermission(givenCanonical, permission))
            {
                var localizer = LocalizerFactory.Create();
                throw new UnauthorizedException(localizer["You do not have permission to perform the action: {0}"]
                    .Format(localizer[permission.ToString()]));
            }
        }

        /// <summary>
        /// Throws an exception if the user is not an administrator.
        /// </summary>
        public async Task RequireAdminPermission()
        {
            if (!IsAdministrator)
            {
                var localizer = LocalizerFactory.Create();
                throw new UnauthorizedException(localizer["You do not have permission to perform the action: {0}"]
                    .Format(localizer["Administration"].Value));
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
