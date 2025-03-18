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
        public bool CanView(string givenPageNavigation);

        /// <summary>
        /// Is the current user allowed to edit?
        /// </summary>
        public bool CanEdit(string givenPageNavigation);

        /// <summary>
        /// Is the current user allowed to perform administrative functions?
        /// </summary>
        public bool CanAdmin();

        /// <summary>
        /// Is the current user allowed to moderate content (such as delete comments, and view moderation tools)?
        /// </summary>
        public bool CanModerate(string givenPageNavigation);

        /// <summary>
        /// Is the current user allowed to moderate content (such as delete comments, and view moderation tools)?
        /// </summary>
        public bool CanModerate();

        /// <summary>
        /// Is the current user allowed to create pages?
        /// </summary>
        public bool CanCreate(string givenPageNavigation);

        /// <summary>
        /// Is the current user allowed to delete unprotected pages?
        /// </summary>
        public bool CanDelete(string givenPageNavigation);

        public DateTime LocalizeDateTime(DateTime datetime);
        public TimeZoneInfo GetPreferredTimeZone();
    }
}
