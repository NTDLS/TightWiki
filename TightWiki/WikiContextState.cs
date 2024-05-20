using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TightWiki.DataModels;
using TightWiki.Exceptions;
using TightWiki.Library;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki
{
    public class WikiContextState
    {
        //Recently moved properties:
        public bool AllowSignup { get; set; }
        public bool IsDebug { get; set; }
        public bool HideFooterComments { get; set; }
        public int PaginationCount { get; set; }
        public int CurrentPage { get; set; }
        public int NextPage { get; set; }
        public int PreviousPage { get; set; }

        public bool CreatePage { get; set; }
        public int CountOfAttachments { get; set; }

        public string? PageName { get; set; }
        public string? TagCloud { get; set; }
        public string? Pages { get; set; } //Rename to Associated Pages
        public string? EmojiName { get; set; } //Move to emoji model.
        public string? AccountName { get; set; } //Move to account model.

        public int MostCurrentRevision { get; set; }
        public int CountOfRevisions { get; set; }

        //Recently moved properties: ↑↑

        public string PageNavigation { get; set; } = string.Empty;
        public string PageRevision { get; set; } = string.Empty;
        public string PathAndQuery { get; set; } = string.Empty;
        public string PageTags { get; set; } = string.Empty;
        public string PageDescription { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public AccountProfile? Profile { get; set; }
        public string Role { get; set; } = string.Empty;
        public int? PageId { get; private set; } = null;
        public int? Revision { get; private set; } = null;
        public List<ProcessingInstruction> ProcessingInstructions { get; set; } = new();
        public bool IsViewingOldVersion => ((Revision ?? 0) > 0);
        public bool IsPageLoaded => ((PageId ?? 0) > 0);

        private Controller? _controller;

        public WikiContextState Hydrate(SignInManager<IdentityUser> signInManager, PageModel pageModel)
        {
            Title = GlobalSettings.Name; //Default the title to the name. This will be replaced when the page is found and loaded.

            HydrateSecurityContext(pageModel.HttpContext, signInManager, pageModel.User);
            return this;
        }

        public WikiContextState Hydrate(SignInManager<IdentityUser> signInManager, Controller controller)
        {
            Title = GlobalSettings.Name; //Default the title to the name. This will be replaced when the page is found and loaded.

            _controller = controller;

            PathAndQuery = controller.Request.GetEncodedPathAndQuery();
            PageNavigation = RouteValue("givenCanonical", "Home");
            PageRevision = RouteValue("pageRevision");

            HydrateSecurityContext(controller.HttpContext, signInManager, controller.User);
            return this;
        }

        private void HydrateSecurityContext(HttpContext httpContext, SignInManager<IdentityUser> signInManager, ClaimsPrincipal user)
        {
            IsAuthenticated = false;

            if (signInManager.IsSignedIn(user))
            {
                try
                {
                    string emailAddress = (user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    IsAuthenticated = user.Identity?.IsAuthenticated == true;
                    if (IsAuthenticated)
                    {
                        var userId = Guid.Parse((user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        Profile = ProfileRepository.GetBasicProfileByUserId(userId);
                        Role = (user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value?.ToString()).EnsureNotNull();
                    }
                }
                catch
                {
                    httpContext.SignOutAsync();
                    if (user.Identity != null)
                    {
                        httpContext.SignOutAsync(user.Identity.AuthenticationType);
                    }
                    throw;
                }
            }
        }

        protected string RouteValue(string key, string defaultValue = "")
        {
            if (_controller.EnsureNotNull().RouteData.Values.ContainsKey(key))
            {
                return _controller.RouteData.Values[key]?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        public DateTime LocalizeDateTime(DateTime datetime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(datetime, GetTimeZone());
        }

        public TimeZoneInfo GetTimeZone()
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
                ProcessingInstructions = new List<ProcessingInstruction>();
            }
        }

        #region Permissions.

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
                    if (ProcessingInstructions.Where(o => o.Instruction == WikiInstruction.Protect).Any())
                    {
                        return (Role == Roles.Administrator
                            || Role == Roles.Moderator);
                    }

                    return (Role == Roles.Administrator
                        || Role == Roles.Contributor
                        || Role == Roles.Moderator);
                }

                return false;
            }
        }

        /// <summary>
        /// Is the current user allowed to perform administrative functions?
        /// </summary>
        public bool CanAdmin =>
            IsAuthenticated
                && Role == Roles.Administrator;

        /// <summary>
        /// Is the current user allowed to moderate content (such as delete comments, and view moderation tools)?
        /// </summary>
        public bool CanModerate =>
            IsAuthenticated
                && (Role == Roles.Administrator
                || Role == Roles.Moderator);

        /// <summary>
        /// Is the current user allowed to create pages?
        /// </summary>
        public bool CanCreate =>
            IsAuthenticated
                && (Role == Roles.Administrator
                || Role == Roles.Contributor
                || Role == Roles.Moderator);

        /// <summary>
        /// Is the current user allowed to delete unprotected pages?
        /// </summary>
        public bool CanDelete
        {
            get
            {
                if (IsAuthenticated)
                {
                    if (ProcessingInstructions.Where(o => o.Instruction.ToLower() == WikiInstruction.Protect.ToString().ToLower()).Any())
                    {
                        return false;
                    }

                    return (Role == Roles.Administrator
                        || Role == Roles.Moderator);
                }

                return false;
            }
        }

        #endregion
    }
}
