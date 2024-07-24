using Microsoft.AspNetCore.Http;

namespace TightWiki.Library.Interfaces
{
    public interface ISessionState
    {
        public IAccountProfile? Profile { get; set; }

        IQueryCollection? QueryString { get; set; }

        /// <summary>
        /// Is the current user (or anonymous) allowed to view?
        /// </summary>
        public bool CanView => true;

        /// <summary>
        /// Is the current user allowed to edit?
        /// </summary>
        public bool CanEdit { get; }

        /// <summary>
        /// Is the current user allowed to perform administrative functions?
        /// </summary>
        public bool CanAdmin { get; }

        /// <summary>
        /// Is the current user allowed to moderate content (such as delete comments, and view moderation tools)?
        /// </summary>
        public bool CanModerate { get; }

        /// <summary>
        /// Is the current user allowed to create pages?
        /// </summary>
        public bool CanCreate { get; }

        /// <summary>
        /// Is the current user allowed to delete unprotected pages?
        /// </summary>
        public bool CanDelete { get; }

        public DateTime LocalizeDateTime(DateTime datetime);
        public TimeZoneInfo GetPreferredTimeZone();
    }
}
