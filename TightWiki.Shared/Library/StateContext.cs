using TightWiki.Shared.Models;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;
using System.Collections.Generic;
using System.Linq;
using static TightWiki.Shared.Library.Constants;

namespace TightWiki.Shared.Library
{
    public class StateContext
    {
        /// <summary>
        /// A more accesible code frendly reference to what's in the viewbag.
        /// </summary>
        public ViewBagConfig Config { get; set; }

        public bool IsAuthenticated { get; set; }
        public User User { get; set; }
        public List<string> Roles { get; set; }
        public int? PageId { get; private set; } = null;
        public int? Revision { get; private set; } = null;
        public List<ProcessingInstruction> ProcessingInstructions { get; set; } = new List<ProcessingInstruction>();

        public void SetPageId(int? pageId, int? revision = null)
        {
            PageId = pageId;
            Revision = revision;
            if (pageId != null)
            {
                var page = PageRepository.GetPageInfoById((int)pageId);

                ProcessingInstructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);

                if (Config.IncludeWikiDescriptionInMeta)
                {
                    Config.PageDescription = page.Description;
                }
                if (Config.IncludeWikiTagsInMeta)
                {
                    Config.PageTags = string.Join(",", PageRepository.GetPageTagsById(page.Id)?.Select(o => o.Tag));
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
                    if (ProcessingInstructions.Where(o => o.Instruction.ToLower() == WikiInstruction.Protect.ToString().ToLower()).Any())
                    {
                        return (Roles.Contains(Constants.Roles.Administrator)
                            || Roles.Contains(Constants.Roles.Moderator));
                    }

                    return (Roles.Contains(Constants.Roles.Administrator)
                        || Roles.Contains(Constants.Roles.Contributor)
                        || Roles.Contains(Constants.Roles.Moderator));
                }

                return false;
            }
        }

        public bool CanModerate =>
            IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Moderator));

        public bool CanCreate =>
            IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));

        public bool CanDelete =>
            IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Moderator));
    }
}
