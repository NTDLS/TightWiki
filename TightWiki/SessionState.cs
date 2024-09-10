using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NTDLS.Helpers;
using System.Security.Claims;
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
        public string Role { get; set; } = string.Empty;
        public Theme UserTheme { get; set; } = new();

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
                            Role = Profile.Role;
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
        /// <param name="pageId"></param>
        /// <param name="revision"></param>
        /// <exception cref="Exception"></exception>
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

        public bool IsMemberOf(string role, string[] roles)
            => roles.Contains(role);

        public void RequireAuthorizedPermission()
        {
            if (!IsAuthenticated) throw new UnauthorizedException();
        }

        public void RequireEditPermission()
        {
            if (!CanEdit) throw new UnauthorizedException();
        }

        public void RequireViewPermission()
        {
            if (!CanView) throw new UnauthorizedException();
        }

        public void RequireAdminPermission()
        {
            if (!CanAdmin) throw new UnauthorizedException();
        }

        public void RequireModeratePermission()
        {
            if (!CanModerate) throw new UnauthorizedException();
        }

        public void RequireCreatePermission()
        {
            if (!CanCreate) throw new UnauthorizedException();
        }

        public void RequireDeletePermission()
        {
            if (!CanDelete) throw new UnauthorizedException();
        }

        /// <summary>
        /// Is the current user (or anonymous) allowed to view?
        /// </summary>
        public bool CanView => true;

        /// <summary>
        /// Is the current user allowed to edit?
        /// </summary>
        public bool CanEdit
        {
            get
            {
                if (IsAuthenticated)
                {
                    if (PageInstructions.Contains(WikiInstruction.Protect))
                    {
                        return IsMemberOf(Role, [Roles.Administrator, Roles.Moderator]);
                    }

                    return IsMemberOf(Role, [Roles.Administrator, Roles.Contributor, Roles.Moderator]);
                }
                return false;
            }
        }

        /// <summary>
        /// Is the current user allowed to perform administrative functions?
        /// </summary>
        public bool CanAdmin =>
            IsAuthenticated && IsMemberOf(Role, [Roles.Administrator]);

        /// <summary>
        /// Is the current user allowed to moderate content (such as delete comments, and view moderation tools)?
        /// </summary>
        public bool CanModerate =>
            IsAuthenticated && IsMemberOf(Role, [Roles.Administrator, Roles.Moderator]);

        /// <summary>
        /// Is the current user allowed to create pages?
        /// </summary>
        public bool CanCreate =>
            IsAuthenticated && IsMemberOf(Role, [Roles.Administrator, Roles.Contributor, Roles.Moderator]);

        /// <summary>
        /// Is the current user allowed to delete unprotected pages?
        /// </summary>
        public bool CanDelete
        {
            get
            {
                if (IsAuthenticated)
                {
                    if (PageInstructions.Contains(WikiInstruction.Protect))
                    {
                        return false;
                    }

                    return IsMemberOf(Role, [Roles.Administrator, Roles.Moderator]);
                }

                return false;
            }
        }

        #endregion

        public DateTime LocalizeDateTime(DateTime datetime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());
        }

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
