using AsapWiki.Shared.Models;
using System.Collections.Generic;

namespace AsapWiki.Shared.Classes
{
    public class StateContext
    {
        public bool IsAuthenticated { get; set; }
        public User User { get; set; }
        public List<string> Roles { get; set; }

        public bool CanView =>
            true;

        public bool CanEdit =>
            IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
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
