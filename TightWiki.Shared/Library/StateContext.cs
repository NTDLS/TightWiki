using System;
using System.Collections.Generic;
using System.Linq;
using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using static TightWiki.Shared.Library.Constants;

namespace TightWiki.Shared.Library
{
    public class StateContext
    {
        public string PageNavigation { get; set; }
        public string PageRevision { get; set; }
        public string PathAndQuery { get; set; }
        public string PageTags { get; set; }
        public string PageDescription { get; set; }
        public string Title { get; set; }
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// This is used when we are authenticated with a 3rd party oauth provider but do not yet have a TightWiki account.
        /// </summary>
        public bool IsPartiallyAuthenticated { get; set; } = false;
        public User User { get; set; }
        public string Role { get; set; }
        public int? PageId { get; private set; } = null;
        public int? Revision { get; private set; } = null;
        public List<ProcessingInstruction> ProcessingInstructions { get; set; } = new List<ProcessingInstruction>();

        public DateTime LocalizeDateTime(DateTime datetime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(datetime, GetTimeZone());
        }

        public TimeZoneInfo GetTimeZone()
        {
            if (User == null)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(Global.DefaultTimeZone);
            }
            return TimeZoneInfo.FindSystemTimeZoneById(User.TimeZone);
        }

        public void SetPageId(int? pageId, int? revision = null)
        {
            PageId = pageId;
            Revision = revision;
            if (pageId != null)
            {
                var page = PageRepository.GetPageInfoById((int)pageId);

                ProcessingInstructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);

                if (Global.IncludeWikiDescriptionInMeta)
                {
                    PageDescription = page.Description;
                }
                if (Global.IncludeWikiTagsInMeta)
                {
                    PageTags = string.Join(",", PageRepository.GetPageTagsById(page.Id)?.Select(o => o.Tag));
                }
            }
            else
            {
                ProcessingInstructions = new List<ProcessingInstruction>();
            }
        }

        public bool IsViewingOldVersion => ((Revision ?? 0) > 0);
        public bool IsPageLoaded => ((PageId ?? 0) > 0);
        public bool CanView => true;

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

        public bool CanAdmin =>
            IsAuthenticated
                && Role == Roles.Administrator;

        public bool CanModerate =>
            IsAuthenticated
                && (Role == Roles.Administrator
                || Role == Roles.Moderator);

        public bool CanCreate =>
            IsAuthenticated
                && (Role == Roles.Administrator
                || Role == Roles.Contributor
                || Role == Roles.Moderator);

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
    }
}
