using System;
using System.Collections.Generic;
using System.Linq;
using TightWiki.Library.DataModels;
using TightWiki.Library.Exceptions;
using TightWiki.Library.Repository;
using static TightWiki.Library.Library.Constants;

namespace TightWiki.Library.Library
{
    public class StateContext
    {
        public string PageNavigation { get; set; } = string.Empty;
        public string PageRevision { get; set; } = string.Empty;
        public string PathAndQuery { get; set; } = string.Empty;
        public string PageTags { get; set; } = string.Empty;
        public string PageDescription { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public AccountProfile? User { get; set; }
        public string Role { get; set; } = string.Empty;
        public int? PageId { get; private set; } = null;
        public int? Revision { get; private set; } = null;
        public List<ProcessingInstruction> ProcessingInstructions { get; set; } = new();
        public bool IsViewingOldVersion => ((Revision ?? 0) > 0);
        public bool IsPageLoaded => ((PageId ?? 0) > 0);

        public DateTime LocalizeDateTime(DateTime datetime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(datetime, GetTimeZone());
        }

        public TimeZoneInfo GetTimeZone()
        {
            if (User == null || string.IsNullOrEmpty(User.TimeZone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(GlobalSettings.DefaultTimeZone);
            }
            return TimeZoneInfo.FindSystemTimeZoneById(User.TimeZone);
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
