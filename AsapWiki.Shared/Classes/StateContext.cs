using AsapWiki.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class StateContext
    {
        public bool IsAuthenticated { get; set; }
        public User User { get; set; }
        public List<string> Roles { get; set; }

        public bool CanEditPage()
        {
            return IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));
        }

        public bool CanCreatePage()
        {
            return IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Contributor)
                || Roles.Contains(Constants.Roles.Moderator));
        }

        public bool CanDeletePage()
        {
            return IsAuthenticated
                && (Roles.Contains(Constants.Roles.Administrator)
                || Roles.Contains(Constants.Roles.Moderator));
        }
    }
}