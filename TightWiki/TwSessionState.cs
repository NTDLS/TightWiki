using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Exceptions;
using TightWiki.Extensions;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;
using TightWiki.Translations;

namespace TightWiki
{
    public class TwSessionState
        : ITwSessionState
    {
        private ITwDatabaseManager? _databaseManager;
        public IQueryCollection? QueryString { get; set; }
        public ILogger<ITwEngine>? Logger { get; private set; }

        #region Authentication.

        public bool IsAuthenticated { get; set; }
        public ITwAccountProfile? Profile { get; set; }
        public bool IsAdministrator { get; set; }
        public Plugin.Models.TwTheme UserTheme { get; set; } = new();
        public List<TwApparentPermission> Permissions { get; set; } = new();

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
        public TwProcessingInstructionCollection PageInstructions { get; set; } = new();
        public TwConfiguration WikiConfiguration { get; set; } = new();
        /// <summary>
        /// The "page" here is more of a "mock page", we use the name for various stuff.
        /// </summary>
        public ITwPage Page { get; set; } = new TwPage();

        #endregion

        /// <summary>
        /// This method is used to hydrate the session state from PageModelBase.
        /// </summary>
        public async Task<TwSessionState> Hydrate(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
            PageModel pageModel, TwConfiguration wikiConfiguration, ITwDatabaseManager databaseManager)
        {
            Page = new TwPage() { Name = WikiConfiguration.Name };
            WikiConfiguration = wikiConfiguration;
            Logger = logger;
            QueryString = pageModel.Request.Query;
            _databaseManager = databaseManager;

            await HydrateSecurityContext(pageModel.HttpContext, signInManager, pageModel.User);
            return this;
        }

        /// <summary>
        /// This method is used to hydrate the session state from WikiControllerBase.
        /// </summary>
        public async Task<TwSessionState> Hydrate(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
            Controller controller, TwConfiguration wikiConfiguration, ITwDatabaseManager databaseManager)
        {
            Page = new TwPage() { Name = WikiConfiguration.Name };
            WikiConfiguration = wikiConfiguration;
            Logger = logger;
            _databaseManager = databaseManager;

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
            if (_databaseManager == null)
                throw new Exception("Database manager is not set on session state.");

            IsAuthenticated = false;

            UserTheme = WikiConfiguration.SystemTheme;

            if (signInManager.IsSignedIn(user))
            {
                try
                {
                    //string emailAddress = (user.Claims.First(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var userId = Guid.Parse((user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        var profile = await _databaseManager.UsersRepository.GetBasicProfileByUserId(userId);
                        if (profile != null)
                        {
                            Profile = profile;
                            IsAdministrator = await _databaseManager.UsersRepository.IsUserMemberOfAdministrators(userId);
                            Permissions = await _databaseManager.UsersRepository.GetApparentAccountPermissions(userId);
                            UserTheme = (await _databaseManager.ConfigurationRepository.GetAllThemes()).SingleOrDefault(o => o.Name == Profile.Theme) ?? WikiConfiguration.SystemTheme;
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

            Permissions = await _databaseManager.UsersRepository.GetApparentRolePermissions(TwRoles.Anonymous);
        }

        /// <summary>
        /// Sets the current context pageId and optionally the revision.
        /// </summary>
        public async Task SetPageId(int? pageId, int? revision = null)
        {
            if (_databaseManager == null)
                throw new Exception("Database manager is not set on session state.");

            Page = new TwPage();
            PageInstructions = new();
            PageTags = string.Empty;

            if (pageId != null)
            {
                Page = await _databaseManager.PageRepository.GetLimitedPageInfoByIdAndRevision((int)pageId, revision)
                    ?? throw new Exception("Page not found");

                PageInstructions = await _databaseManager.PageRepository.GetPageProcessingInstructionsByPageId(Page.Id);

                if (WikiConfiguration.IncludeWikiTagsInMeta)
                {
                    PageTags = string.Join(",", (await _databaseManager.PageRepository.GetPageTagsById(Page.Id))
                        ?.Select(o => o.Tag) ?? []);
                }
            }
        }

        #region Permissions.

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public async Task<bool> HoldsPermission(Plugin.TwPermission[] permissions)
            => await HoldsPermission(Page?.Navigation, permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public async Task<bool> HoldsPermission(Plugin.TwPermission permission)
            => await HoldsPermission(Page?.Navigation, permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public async Task<bool> HoldsPermission(string? givenCanonical, Plugin.TwPermission permission)
            => await HoldsPermission(givenCanonical, [permission]);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public async Task<bool> HoldsPermission(string? givenCanonical, Plugin.TwPermission[] permissions)
        {
            if (_databaseManager == null)
                throw new Exception("Database manager is not set on session state.");

            if (IsAdministrator)
            {
                return true;
            }

            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [givenCanonical, Profile?.UserId, string.Join("|", permissions).ToLowerInvariant()]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                TwPage? page = null;
                string? inferredNamespace = null;

                if (givenCanonical != null)
                {
                    var navigation = new TwNamespaceNavigation(givenCanonical);
                    page = await _databaseManager.PageRepository.GetPageInfoByNavigation(navigation.Canonical);

                    // Capture namespace from the URL so deny rules still apply even when the page doesn't exist.
                    if (!string.IsNullOrEmpty(navigation.Namespace))
                    {
                        inferredNamespace = navigation.Namespace;
                    }
                }

                return TwPermissionEvaluator.HoldsAny(Permissions, permissions, page, inferredNamespace);
            });
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
        public async Task RequirePermission(string? givenCanonical, Plugin.TwPermission[] permissions)
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
        public async Task RequirePermission(string? givenCanonical, Plugin.TwPermission permission)
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
                return TimeZoneInfo.FindSystemTimeZoneById(WikiConfiguration.DefaultTimeZone);
            }
            return TimeZoneInfo.FindSystemTimeZoneById(Profile.TimeZone);
        }
    }
}
