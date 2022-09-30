using AsapWiki.Shared.Models;
using System.Collections.Generic;

namespace AsapWiki.Shared.Classes
{
    public class StateContext
    {
        public bool IsAuthenticated { get; set; }
        public User User { get; set; }
        public List<string> Roles { get; set; }
        public int? PageId { get; set; } = null;

        public bool IsPageLoaded => ((PageId ?? 0) > 0);
        public bool CanView => true;

        public bool CanEdit =>
            IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));

        public bool CanCreate =>
            IsAuthenticated && CanEdit
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));

        public bool CanDelete =>
            IsAuthenticated && CanCreate
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Moderator));
    }
}
