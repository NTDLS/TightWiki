using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TightWiki.Configuration;
using TightWiki.Exceptions;
using TightWiki.Interfaces;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki
{
    public class WikiContextState : IWikiContext
    {
        #region Authentication.

        public bool IsAuthenticated { get; set; }
        public AccountProfile? Profile { get; set; }
        public string Role { get; set; } = string.Empty;
        public Theme UserTheme { get; set; } = new();

        #endregion

        #region Current Page.

        public bool ShouldCreatePage { get; set; }
        public string PageNavigation { get; set; } = string.Empty;
        public string PageNavigationEscaped { get; set; } = string.Empty;
        public string PageRevision { get; set; } = string.Empty;
        public string PathAndQuery { get; set; } = string.Empty;
        public string PageTags { get; set; } = string.Empty;
        public string PageDescription { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? PageId { get; private set; } = null;
        public int? Revision { get; private set; } = null;
        public ProcessingInstructionCollection ProcessingInstructions { get; set; } = new();
        public bool IsViewingOldVersion => (Revision ?? 0) > 0;
        public bool IsPageLoaded => (PageId ?? 0) > 0;

        #endregion

        public WikiContextState Hydrate(SignInManager<IdentityUser> signInManager, PageModel pageModel)
        {
            Title = GlobalSettings.Name; //Default the title to the name. This will be replaced when the page is found and loaded.

            HydrateSecurityContext(pageModel.HttpContext, signInManager, pageModel.User);
            return this;
        }

        public WikiContextState Hydrate(SignInManager<IdentityUser> signInManager, Controller controller)
        {
            Title = GlobalSettings.Name; //Default the title to the name. This will be replaced when the page is found and loaded.

            PathAndQuery = controller.Request.GetEncodedPathAndQuery();
            PageNavigation = RouteValue("givenCanonical", "Home");
            PageNavigationEscaped = Uri.EscapeDataString(PageNavigation);
            PageRevision = RouteValue("pageRevision");

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

            UserTheme = GlobalSettings.SystemTheme;

            if (signInManager.IsSignedIn(user))
            {
                try
                {
                    string emailAddress = (user.Claims.First(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    IsAuthenticated = user.Identity?.IsAuthenticated == true;
                    if (IsAuthenticated)
                    {
                        var userId = Guid.Parse((user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        Profile = UsersRepository.GetBasicProfileByUserId(userId);
                        Role = Profile.Role;
                        UserTheme = ConfigurationRepository.GetAllThemes().SingleOrDefault(o => o.Name == Profile.Theme) ?? GlobalSettings.SystemTheme;
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

        public DateTime LocalizeDateTime(DateTime datetime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());
        }

        public TimeZoneInfo GetPreferredTimeZone()
        {
            if (Profile == null || string.IsNullOrEmpty(Profile.TimeZone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(GlobalSettings.DefaultTimeZone);
            }
            return TimeZoneInfo.FindSystemTimeZoneById(Profile.TimeZone);
        }

        /// <summary>
        /// Sets the current context pageId and optionally the revision.
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="revision"></param>
        /// <exception cref="Exception"></exception>
        public void SetPageId(int? pageId, int? revision = null)
        {
            PageId = pageId;
            Revision = revision;
            if (pageId != null)
            {
                var page = PageRepository.GetPageInfoById((int)pageId) ?? throw new Exception("Page not found");

                ProcessingInstructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);

                Title = $"{page.Name}";

                if (GlobalSettings.IncludeWikiDescriptionInMeta)
                {
                    PageDescription = page.Description;
                }
                if (GlobalSettings.IncludeWikiTagsInMeta)
                {
                    var tags = PageRepository.GetPageTagsById(page.Id)?.Select(o => o.Tag).ToList() ?? new();
                    PageTags = string.Join(",", tags);
                }
            }
            else
            {
                ProcessingInstructions = new();
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
                    if (ProcessingInstructions.Contains(WikiInstruction.Protect))
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
                    if (ProcessingInstructions.Contains(WikiInstruction.Protect))
                    {
                        return false;
                    }

                    return IsMemberOf(Role, [Roles.Administrator, Roles.Moderator]);
                }

                return false;
            }
        }

        #endregion
    }
}
